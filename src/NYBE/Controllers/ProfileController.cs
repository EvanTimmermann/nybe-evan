﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NYBE.Data;
using NYBE.Models;
using NYBE.Models.DataModels;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NYBE.Controllers
{
    public class ProfileController: Controller
    {
        private readonly ApplicationDbContext ctx;
        private readonly UserManager<ApplicationUser> usrCtx;
        public ProfileController(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext)
        {
            ctx = dbContext;
            usrCtx = userManager;
        }

        // 
        // GET: /Profile/UserID
        [HttpGet]
        public async Task<IActionResult> Index(string UserId)
        {
            ApplicationUser user;
            ProfileViewModel view = new ProfileViewModel();
            user = await GetCurrentUserAsync();
            view.ownProfile = (user.Id == UserId) || UserId == null;
            if (!view.ownProfile)
            {
                user = await usrCtx.FindByIdAsync(UserId);
            }
            view.isAdmin = await usrCtx.IsInRoleAsync(user, "Admin");
            view.name = user.FirstName + " " + user.LastName;
            view.email = user.Email;
            view.phone = user.PhoneNumber;
            view.rating = user.Rating;

            switch (user.PreferredContact)
            {
                case 0:
                    view.preferredContact = "Email";
                    break;
                case 1:
                    view.preferredContact = "Call";
                    break;
                case 2:
                    view.preferredContact = "Text";
                    break;
            }
            view.school = ctx.Schools.Where(a => a.ID == user.SchoolID).FirstOrDefault();

            // get all the user's (open) book listings include the book and course objects to view in the table
            view.listings = ctx.BookListings.Include("Book").Include("Course").Where(a => user.Id == a.ApplicationUserID && a.Type == 0 && a.Status == 0).ToList();

            // get the sold transaction history for this user
            view.soldTransactions = ctx.TransactionLogs.Include("Buyer").Include("Book").
                Where(a => 0 == a.Status && user.Id == a.SellerID).
                OrderByDescending(a => a.TransDate).ToList();

            // get the bought transaction history for this user
            view.boughtTransactions = ctx.TransactionLogs.Include("Seller").Include("Book").
                Where(a => 0 == a.Status && user.Id == a.BuyerID).
                OrderByDescending(a => a.TransDate).ToList();

            //view.wishList = ctx.BookListings.Include("User").Include("User.School").Include("Book").Include("Course").Where(a => user.Id == a.ApplicationUserID && a.Type == 1).ToList();
            List<BookListing> wishList = ctx.BookListings.Include("User").Include("User.School").Include("Book").Include("Course").Where(a => user.Id == a.ApplicationUserID && a.Type == 1 && a.Status == 0).ToList();
            view.wishList = new Dictionary<BookListing, bool>();
            foreach (BookListing b in wishList)
            {
                if(ctx.BookListings.Where(i => i.Book.ID == b.Book.ID && i.Type == 0).FirstOrDefault() != null)
                {
                    view.wishList.Add(b, true);
                } else
                {
                    view.wishList.Add(b, false);
                }
            }

            // Get the pending transactions for this user.
            view.pendingTransactions = ctx.TransactionLogs.Include("Seller").Include("Book").Where(a => a.Status == 1 && a.BuyerID == user.Id).OrderByDescending(a => a.TransDate).ToList();
            // Populate list with corresponding book listings. (gross)
            view.pendingBookListings = new List<BookListing>();
            foreach (TransactionLog log in view.pendingTransactions)
            {
                view.pendingBookListings.Add(ctx.BookListings.Where(a => a.ApplicationUserID == log.SellerID && a.BookID == log.BookID && a.Condition == log.Condition && a.AskingPrice == log.SoldPrice).FirstOrDefault());
            }

            return View(view);
        }

        [HttpGet]
        public ActionResult EditListing(int id)
        {
            var viewModel = new EditListingViewModel();
            var temp = ctx.BookListings.Include("Book").Include("Course").Where(a => a.ID == id && a.Status == 0).FirstOrDefault();
            viewModel.courses = ctx.Courses.ToList();
            viewModel.courseID = temp.CourseID;
            viewModel.ID = id;
            viewModel.book = temp.Book;
            viewModel.course = temp.Course;
            viewModel.condition = temp.Condition;
            viewModel.price = temp.AskingPrice;
            return View("EditListing", viewModel);
        }

        [HttpPost]
        public async Task<ActionResult> EditListing(EditListingViewModel viewModel)
        {
            var temp = ctx.BookListings.Where(a => a.ID == viewModel.ID && a.Status == 0).FirstOrDefault();
            // Find all open transactions and remove them
            var openLogs = ctx.TransactionLogs.Where(a => a.SellerID == temp.ApplicationUserID && a.BookID == temp.BookID && a.Condition == temp.Condition && a.SoldPrice == temp.AskingPrice);
            foreach (TransactionLog log in openLogs)
            {
                ctx.TransactionLogs.Remove(log);
            }

            Course newCourse = new Course();

            temp.Condition = viewModel.condition;
            temp.CourseID = viewModel.courseID;
            temp.AskingPrice = viewModel.price;
            // If they created a new course, add the course to the database.
            if (viewModel.newCourse) {
                newCourse.Dept = viewModel.courseDept;
                newCourse.CourseNum = viewModel.courseID;
                newCourse.Name = viewModel.courseName;
                ApplicationUser user = await usrCtx.GetUserAsync(HttpContext.User);
                newCourse.SchoolID = user.SchoolID;
                ctx.Courses.Add(newCourse);
                temp.Course = newCourse;
            }

            ctx.Update(temp);
            ctx.SaveChanges();

            // Put it after the new course is added and saved into the database so it receives an ID.
            if (viewModel.newCourse) {
                // Connect the book and the new course if it doesn't already exist.
                if (!ctx.BookToCourses.Where(a => a.BookID == viewModel.book.ID && a.CourseID == newCourse.ID).Any()) {
                    BookToCourse bookToCourse = new BookToCourse();
                    bookToCourse.BookID = viewModel.book.ID;
                    bookToCourse.CourseID = newCourse.ID;
                    ctx.BookToCourses.Add(bookToCourse);
                    ctx.SaveChanges();
                }
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult DeleteListing(int? id)
        {
            if (id == null) return NotFound();

            var viewModel = new EditListingViewModel();
            var temp = ctx.BookListings.Include("Book").Include("Course").Where(a => a.ID == id && a.Status == 0).FirstOrDefault();
            if (temp == null) return NotFound();
            viewModel.ID = temp.ID;
            viewModel.book = temp.Book;
            viewModel.course = temp.Course;
            viewModel.condition = temp.Condition;
            viewModel.price = temp.AskingPrice;
            return View("DeleteListing", viewModel);
        }

        [HttpPost]
        public ActionResult DeleteListing(EditListingViewModel viewModel)
        {
            var temp = ctx.BookListings.Where(a => a.ID == viewModel.ID && a.Status == 0).FirstOrDefault();
            ctx.BookListings.Remove(temp);
            ctx.SaveChanges();

            return RedirectToAction("Index");
        }

        private Task<ApplicationUser> GetCurrentUserAsync()
        {
            return usrCtx.GetUserAsync(HttpContext.User);
        }
    }
}
