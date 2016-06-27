using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using NotificationTest.Models;

namespace NotificationTest.Controllers
{
    public class InstallationController : Controller
    {
        private InstallationContext db = new InstallationContext();

        // GET: Installation
        public ActionResult Index()
        {
            return View(db.InstallationModels.ToList());
        }

        // GET: Installation/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            InstallationModel installationModel = db.InstallationModels.Find(id);
            if (installationModel == null)
            {
                return HttpNotFound();
            }
            return View(installationModel);
        }

        // GET: Installation/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Installation/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Comment,InstallationId,PushChannel,PushChannelExpired,Platform,ExpirationTime")] InstallationModel installationModel)
        {
            if (ModelState.IsValid)
            {
                db.InstallationModels.Add(installationModel);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(installationModel);
        }

        // GET: Installation/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            InstallationModel installationModel = db.InstallationModels.Find(id);
            if (installationModel == null)
            {
                return HttpNotFound();
            }
            return View(installationModel);
        }

        // POST: Installation/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Comment,InstallationId,PushChannel,PushChannelExpired,Platform,ExpirationTime")] InstallationModel installationModel)
        {
            if (ModelState.IsValid)
            {
                db.Entry(installationModel).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(installationModel);
        }

        // GET: Installation/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            InstallationModel installationModel = db.InstallationModels.Find(id);
            if (installationModel == null)
            {
                return HttpNotFound();
            }
            return View(installationModel);
        }

        // POST: Installation/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            InstallationModel installationModel = db.InstallationModels.Find(id);
            db.InstallationModels.Remove(installationModel);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
