﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using TellToAsk.Models;
using TellToAsk.Data;
using TellToAsk.Model;
using TellToAsk.Areas.LoggedUser.Models;

namespace TellToAsk.Controllers
{
    [Authorize]
    public class AccountController : BaseController
    {
        public AccountController()
            : this(null, new UowData())
        {
            IdentityManager = new AuthenticationIdentityManager(new IdentityStore(new TellToAskContext()));
        }

        public AccountController(AuthenticationIdentityManager manager, IUowData data)
            : base(data)
        {
            IdentityManager = manager;
        }

        public AuthenticationIdentityManager IdentityManager { get; private set; }

        private Microsoft.Owin.Security.IAuthenticationManager AuthenticationManager {
            get {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                // Validate the password
                IdentityResult result = await IdentityManager.Authentication.CheckPasswordAndSignInAsync(AuthenticationManager, model.UserName, model.Password, model.RememberMe);
                if (result.Success)
                {
                    return RedirectToLocal(returnUrl);
                }
                else
                {
                    AddErrors(result);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {

            ViewBag.genders = this.PopulateGendersList();
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model, int[] categories)
        {
            //PopulateGenders();
            List<Category> appruvedCategories = new List<Category>();
            List<CategoryModel> appruvedCatModels = new List<CategoryModel>();
            string errorMsg = "";
            model.Gender = (Gender)model.Gender;
            if (categories != null && categories.Length > 0)
            {
                foreach (var catId in categories)
                {
                    var cat = this.Data.Categories.GetById(catId);
                    if (cat != null)
                    {
                        if (DateTime.Now.AddYears((-1) * (int)cat.AgeRating) >= DateTime.Parse(model.BirthDate))
                        {
                            appruvedCategories.Add(cat);

                        }
                        else
                        {
                            errorMsg += cat.Name + " ; ";
                        }
                    }
                }
                appruvedCatModels = appruvedCategories.AsQueryable().Select(CategoryModel.FromCategory).ToList();
            }
            if (appruvedCategories.Count() >= 3)
            {
                if (ModelState.IsValid && appruvedCategories.Count() >= 3)
                {
                    // Create a local login before signing in the user
                    var user = new ApplicationUser
                    {
                        UserName = model.UserName,
                        BirthDate = DateTime.Parse(model.BirthDate),
                        Gender = model.Gender,
                    };

                    var result = await IdentityManager.Users.CreateLocalUserAsync(user, model.Password);
                    if (result.Success)
                    {
                        //add user to role
                        await IdentityManager.Authentication.SignInAsync(AuthenticationManager, user.Id, isPersistent: false);
                        Task<IRole> getRoleTask = IdentityManager.Roles.FindRoleByNameAsync("User");
                        var userRole = await getRoleTask;
                        await IdentityManager.Roles.AddUserToRoleAsync(user.Id, userRole.Id);

                        string uId = user.Id;
                        var user2 = this.Data.Users.All().FirstOrDefault(u => u.Id == uId);
                        foreach (var cat in appruvedCategories)
                        {
                            if (cat != null)
                            {
                                user2.Categories.Add(cat);
                            }
                        }
                        this.Data.SaveChanges();

                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        AddErrors(result);
                        ViewBag.genders =  this.PopulateGendersList();
                        return View(model);
                    }
                }
            }
            else
            {
                ViewBag.genders = this.PopulateGendersList();
                ViewBag.error = (string.IsNullOrEmpty(errorMsg) ? "Select at least 3 categories" : "Categories NOT suitable for your age are:  " + errorMsg);
                ViewBag.catselected = appruvedCatModels;
                return View(model);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/Disassociate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Disassociate(string loginProvider, string providerKey)
        {
            string message = null;
            IdentityResult result = await IdentityManager.Logins.RemoveLoginAsync(User.Identity.GetUserId(), loginProvider, providerKey);
            if (result.Success)
            {
                message = "The external login was removed.";
            }
            else
            {
                message = result.Errors.FirstOrDefault();
            }

            return RedirectToAction("Manage", new { Message = message });
        }

        //
        // GET: /Account/Manage
        public async Task<ActionResult> Manage(string message)
        {
            ViewBag.StatusMessage = message ?? "";
            ViewBag.HasLocalPassword = await IdentityManager.Logins.HasLocalLoginAsync(User.Identity.GetUserId());
            ViewBag.ReturnUrl = Url.Action("Manage");

            return View();
        }

        //
        // POST: /Account/Manage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Manage(ManageUserViewModel model, int[] categories)
        {

            string userId = User.Identity.GetUserId();
            bool hasLocalLogin = await IdentityManager.Logins.HasLocalLoginAsync(userId);
            ViewBag.HasLocalPassword = hasLocalLogin;
            ViewBag.ReturnUrl = Url.Action("Manage");
            if (hasLocalLogin)
            {
                if (ModelState.IsValid)
                {
                    IdentityResult result = await IdentityManager.Passwords.ChangePasswordAsync(User.Identity.GetUserName(), model.OldPassword, model.NewPassword);
                    if (result.Success)
                    {
                        return RedirectToAction("Manage", new { Message = "Your password has been changed." });
                    }
                    else
                    {
                        AddErrors(result);
                    }
                }
            }
            else
            {
                // User does not have a local password so remove any validation errors caused by a missing OldPassword field
                ModelState state = ModelState["OldPassword"];
                if (state != null)
                {
                    state.Errors.Clear();
                }

                if (ModelState.IsValid)
                {
                    // Create the local login info and link it to the user
                    IdentityResult result = await IdentityManager.Logins.AddLocalLoginAsync(userId, User.Identity.GetUserName(), model.NewPassword);
                    if (result.Success)
                    {
                        return RedirectToAction("Manage", new { Message = "Your password has been set." });
                    }
                    else
                    {
                        AddErrors(result);
                    }
                }
            }
            // If we got this far, something failed, redisplay form
            return View( model);
        }

        //
        // GET: /Account/ManageUser
        public async Task<ActionResult> ManageUser(string message)
        {
          
            var userName = this.User.Identity.Name;
            var user = this.Data.Users.All().FirstOrDefault(u => u.UserName == userName);
            PopulateGenders(user.Gender);
            var userCats = user.Categories.AsQueryable().Select(CategoryModel.FromCategory).ToList();
            ViewBag.catSelected = userCats;
            ViewBag.Points = user.Points;
            ViewBag.BirthDate =user.BirthDate.Value.ToString("yyyy-MM-dd");

            ViewBag.StatusMessage = message ?? "";
            ViewBag.HasLocalPassword = await IdentityManager.Logins.HasLocalLoginAsync(User.Identity.GetUserId());
            ViewBag.ReturnUrl = Url.Action("ManageUser");

            return View("_ChangeCategiriesPartial");
        }

        //
        // POST: /Account/ManageUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ManageUser(ManageUserProfilViewModel model, int[] categories)
        {
           
            List<Category> appruvedCategories = new List<Category>();
            List<CategoryModel> appruvedCatModels = new List<CategoryModel>();
            string errorMsg = "";
            model.Gender = (Gender)model.Gender;
            DateTime userBd = new DateTime(1,1,1);
            var userName = this.User.Identity.Name;
            var user = this.Data.Users.All().FirstOrDefault(u => u.UserName == userName);

            bool hasNewBd = false;
            if (model.BirthDate != null  )
	        {
                hasNewBd=DateTime.TryParse(model.BirthDate, out  userBd);
	        }
            if (!hasNewBd)
            {
                userBd = user.BirthDate.Value; 
            }

            if (categories != null && categories.Length > 0 && ModelState.IsValid)
            {
                foreach (var catId in categories)
                {
                    var cat = this.Data.Categories.GetById(catId);
                    if (cat != null)
                    {
                        if (DateTime.Now.AddYears((-1) * (int)cat.AgeRating) >= userBd)
                        {
                            appruvedCategories.Add(cat);
                        }
                        else
                        {
                            errorMsg += cat.Name + " ; ";
                        }
                    }
                }

                appruvedCatModels = appruvedCategories.AsQueryable().Select(CategoryModel.FromCategory).ToList();

                if (appruvedCategories.Count() >= 3)
                {
                    string userId = User.Identity.GetUserId();
                    bool hasLocalLogin = await IdentityManager.Logins.HasLocalLoginAsync(userId);
                    ViewBag.HasLocalPassword = hasLocalLogin;
                    ViewBag.ReturnUrl = Url.Action("ManageUser");
                    if (hasLocalLogin)
                    {
                        if (ModelState.IsValid)
                        {
                            IdentityResult result = await IdentityManager.Authentication.CheckPasswordAndSignInAsync(AuthenticationManager, userName, model.OldPassword, false);
                            if (result.Success)
                            {
                                if (hasNewBd)
                                {
                                    user.BirthDate = DateTime.Parse(model.BirthDate);
                                }
                                user.Gender = model.Gender;

                                var oldCats = user.Categories.ToList();

                                foreach (var cat in oldCats)
                                {
                                    user.Categories.Remove(cat);
                                }

                                foreach (var cat in appruvedCategories)
                                {
                                    if (cat != null)
                                    {
                                        user.Categories.Add(cat);
                                    }
                                }
                                //=======
                                this.Data.SaveChanges();
                                 PopulateGenders(model.Gender);
                                return RedirectToAction("ManageUser", new { Message = "Your Profil has been updated." });
                            }
                            else
                            {
                                AddErrors(result);
                            }
                        }
                    }
                    PopulateGenders(model.Gender);
                    // If we got this far, something failed, redisplay form
                    return View("_ChangeCategiriesPartial", model);
                }
            }

            PopulateGenders(model.Gender);
            ViewBag.error = (string.IsNullOrEmpty(errorMsg) ? "Select at least 3 categories" : "Categories NOT suitable for your age are:  " + errorMsg);
            ViewBag.catselected = appruvedCatModels;
            return View("_ChangeCategiriesPartial", model);

            // If we got this far, something failed, redisplay form
            // return View("_ChangeCategiriesPartial", model);
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { loginProvider = provider, ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string loginProvider, string returnUrl)
        {
            ClaimsIdentity id = await IdentityManager.Authentication.GetExternalIdentityAsync(AuthenticationManager);
            // Sign in this external identity if its already linked
            IdentityResult result = await IdentityManager.Authentication.SignInExternalIdentityAsync(AuthenticationManager, id);
            if (result.Success) 
            {
                return RedirectToLocal(returnUrl);
            }
            else if (User.Identity.IsAuthenticated)
            {
                // Try to link if the user is already signed in
                result = await IdentityManager.Authentication.LinkExternalIdentityAsync(id, User.Identity.GetUserId());
                if (result.Success)
                {
                    return RedirectToLocal(returnUrl);
                }
                else 
                {
                    return View("ExternalLoginFailure");
                }
            }
            else
            {
                // Otherwise prompt to create a local user
                ViewBag.ReturnUrl = returnUrl;
                ViewBag.LoginProvider = loginProvider;
                return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { UserName = id.Name });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Manage");
            }
            
            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                IdentityResult result = await IdentityManager.Authentication.CreateAndSignInExternalUserAsync(AuthenticationManager, new User(model.UserName));
                if (result.Success)
                {
                    return RedirectToLocal(returnUrl);
                }
                else
                {
                    AddErrors(result);
                }
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut();
            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        [AllowAnonymous]
        [ChildActionOnly]
        public ActionResult ExternalLoginsList(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return (ActionResult)PartialView("_ExternalLoginsListPartial", new List<AuthenticationDescription>(AuthenticationManager.GetExternalAuthenticationTypes()));
        }

        [ChildActionOnly]
        public ActionResult RemoveAccountList()
        {
            return Task.Run(async () =>
            {
                var linkedAccounts = new List<IUserLogin>(await IdentityManager.Logins.GetLoginsAsync(User.Identity.GetUserId()));
                ViewBag.ShowRemoveButton = linkedAccounts.Count > 1;
                return (ActionResult)PartialView("_RemoveAccountPartial", linkedAccounts);
            }).Result;
        }

        protected override void Dispose(bool disposing) {
            if (disposing && IdentityManager != null) {
                IdentityManager.Dispose();
                IdentityManager = null;
            }
            base.Dispose(disposing);
        }


        private void PopulateGenders(Gender currentGender)
        {
            IList<SelectListItem> genList = new List<SelectListItem>();
            foreach (Gender gen in Enum.GetValues(typeof(Gender)))
            {
                SelectListItem item = new SelectListItem()
                {
                    Selected = false,
                    Text = gen.ToString(),
                    Value = ((int)gen).ToString(),
                };

                if (gen == currentGender)
                {
                    item.Selected = true;
                }

                genList.Add(item);
            }

            ViewBag.genders = genList;
        }


        #region Helpers
        private void AddErrors(IdentityResult result) {
            foreach (var error in result.Errors) {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        private class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUrl)
            {
                LoginProvider = provider;
                RedirectUrl = redirectUrl;
            }

            public string LoginProvider { get; set; }
            public string RedirectUrl { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                context.HttpContext.GetOwinContext().Authentication.Challenge(new AuthenticationProperties() { RedirectUrl = RedirectUrl }, LoginProvider);
            }
        }
        #endregion
    }
}