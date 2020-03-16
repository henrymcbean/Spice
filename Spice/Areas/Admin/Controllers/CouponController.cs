using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Utility;

namespace Spice.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.ManagerUser)]
    public class CouponController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CouponController(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task<IActionResult> Index()
        {
            return View(await _db.Coupon.ToListAsync());
        }

        // GET - CREATE
        public ActionResult Create()
        {
            return View();
        }

        // POST - CREATE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Coupon coupon)
        {
            if (ModelState.IsValid)
            {
                var files = Request.Form.Files;

                if (files.Count > 0)
                {
                    byte[] p1 = null;
                    using (var fs1 = files[0].OpenReadStream())
                    {
                        using (var ms1 = new MemoryStream())
                        {
                            fs1.CopyTo(ms1);
                            p1 = ms1.ToArray();
                        }
                    }
                    coupon.Picture = p1;
                }

                _db.Coupon.Add(coupon);
                await _db.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(coupon);
        }

        // GET - EDIT
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var couponFromDB = await _db.Coupon.FindAsync(id);

            if (couponFromDB == null)
            {
                return NotFound();
            }

            return View(couponFromDB);
        }

        // POST - EDIT
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditConfirmed(int? id, Coupon coupon)
        {
            if (ModelState.IsValid)
            {
                var couponFromDb = await _db.Coupon.FindAsync(id);

                if (couponFromDb != null)
                {
                    var files = Request.Form.Files;

                    if (files.Count > 0)
                    {
                        byte[] p1 = null;
                        using (var fs1 = files[0].OpenReadStream())
                        {
                            using (var ms1 = new MemoryStream())
                            {
                                fs1.CopyTo(ms1);
                                p1 = ms1.ToArray();
                            }
                        }
                        couponFromDb.Picture = p1;
                    }

                    couponFromDb.Name = coupon.Name;
                    couponFromDb.CouponType = coupon.CouponType;
                    couponFromDb.Discount = coupon.Discount;
                    couponFromDb.MinimumAmount = coupon.MinimumAmount;
                    couponFromDb.isActive = coupon.isActive;

                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    return NotFound();
                }
            }

            return View(coupon);
        }

        // GET - DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var couponFromDB = await _db.Coupon.FindAsync(id);

            if (couponFromDB == null)
            {
                return NotFound();
            }

            return View(couponFromDB);
        }

        // POST - DETAILS
        [HttpPost, ActionName("Details")]
        [ValidateAntiForgeryToken]
        public IActionResult DetailsConfirmed(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Edit), new { id });
        }

        // GET - DELETE
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var couponFromDB = await _db.Coupon.FindAsync(id);

            if (couponFromDB == null)
            {
                return NotFound();
            }

            return View(couponFromDB);
        }

        // POST - DELETE
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            var couponFromDb = await _db.Coupon.FirstOrDefaultAsync(u => u.Id == id);

            if (couponFromDb == null)
            {
                return View();
            }

            _db.Coupon.Remove(couponFromDb);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}