using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Spice.Models.ViewModels;
using Spice.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Spice.Models;
using Spice.Utility;
using Microsoft.AspNetCore.Http;

namespace Spice.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;

        [BindProperty]
        public OrdersDetailsCartViewModel DetailsCart { get; set; }

        public CartController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            DetailsCart = new OrdersDetailsCartViewModel()
            {
                OrderHeader = new Models.OrderHeader()
            };

            DetailsCart.OrderHeader.OrderTotal = 0;
            DetailsCart.OrderHeader.OrderTotalOriginal = 0;

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var cart = _db.ShoppingCart.Where(c => c.ApplicationUserId == claim.Value);

            if (cart != null)
            {
                DetailsCart.ListCart = cart.ToList();
            }

            foreach(var item in DetailsCart.ListCart)
            {
                item.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == item.MenuItemId);
                DetailsCart.OrderHeader.OrderTotal += item.Count * item.MenuItem.Price;
                item.MenuItem.Description = SD.ConvertToRawHtml(item.MenuItem.Description);

                if (item.MenuItem.Description.Length > 100)
                {
                    item.MenuItem.Description = item.MenuItem.Description.Substring(0, 99) + "...";
                }
            }

            DetailsCart.OrderHeader.OrderTotalOriginal = DetailsCart.OrderHeader.OrderTotal;

            if (HttpContext.Session.GetString(SD.ssCouponCode) != null)
            {
                DetailsCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
                var couponFromDb = await _db.Coupon.FirstOrDefaultAsync(c => c.Name.ToLower().Trim() == DetailsCart.OrderHeader.CouponCode.ToLower().Trim());
                DetailsCart.OrderHeader.OrderTotal = SD.DiscountedPrice(couponFromDb, DetailsCart.OrderHeader.OrderTotalOriginal);
            }

            return View(DetailsCart);
        }

        public IActionResult AddCoupon()
        {
            if (DetailsCart.OrderHeader.CouponCode == null)
            {
                DetailsCart.OrderHeader.CouponCode = "";
            }

            HttpContext.Session.SetString(SD.ssCouponCode, DetailsCart.OrderHeader.CouponCode);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult RemoveCoupon()
        {
            HttpContext.Session.SetString(SD.ssCouponCode, string.Empty);

            return RedirectToAction(nameof(Index));
        }
    }
}