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


            DetailsCart.OrderHeader.OrderTotal = Math.Round(DetailsCart.OrderHeader.OrderTotal, 2);
            DetailsCart.OrderHeader.OrderTotalOriginal = DetailsCart.OrderHeader.OrderTotal;

            if (HttpContext.Session.GetString(SD.ssCouponCode) != null)
            {
                DetailsCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
                var couponFromDb = await _db.Coupon.FirstOrDefaultAsync(c => c.Name.ToLower().Trim() == DetailsCart.OrderHeader.CouponCode.ToLower().Trim());
                DetailsCart.OrderHeader.OrderTotal = SD.DiscountedPrice(couponFromDb, DetailsCart.OrderHeader.OrderTotalOriginal);
            }

            return View(DetailsCart);
        }

        public async Task<IActionResult> Summary()
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
            ApplicationUser applicationUser = await _db.ApplicationUser.Where(c => c.Id == claim.Value).FirstOrDefaultAsync();

            if (cart != null)
            {
                DetailsCart.ListCart = cart.ToList();
            }

            foreach (var item in DetailsCart.ListCart)
            {
                item.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == item.MenuItemId);
                DetailsCart.OrderHeader.OrderTotal += item.Count * item.MenuItem.Price;
            }


            DetailsCart.OrderHeader.OrderTotal = Math.Round(DetailsCart.OrderHeader.OrderTotal, 2);
            DetailsCart.OrderHeader.OrderTotalOriginal = DetailsCart.OrderHeader.OrderTotal;

            DetailsCart.OrderHeader.PickUpName = applicationUser.Name;
            DetailsCart.OrderHeader.PhoneNumber = applicationUser.PhoneNumber;
            DetailsCart.OrderHeader.PickupDate = DateTime.Now;
            DetailsCart.OrderHeader.PickupTime = DateTime.Now;


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

        public async Task<IActionResult> Plus(int cartId)
        {
            var cartItemFromDb = await _db.ShoppingCart.Where(c => c.Id == cartId).FirstOrDefaultAsync();

            if (cartItemFromDb != null)
            { 
                cartItemFromDb.Count += 1;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Minus(int cartId)
        {
            var cartItemFromDb = await _db.ShoppingCart.Where(c => c.Id == cartId).FirstOrDefaultAsync();

            if (cartItemFromDb.Count == 1)
            {
                _db.ShoppingCart.Remove(cartItemFromDb);
                await _db.SaveChangesAsync();

                var cnt = _db.ShoppingCart.Where(c => c.ApplicationUserId == cartItemFromDb.ApplicationUserId).ToList().Count;
                HttpContext.Session.SetInt32(SD.ssShoppingCartCount, cnt);

                return RedirectToAction(nameof(Index));
            }
            else
            { 
                cartItemFromDb.Count -= 1;
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Remove(int cartId)
        {
            var cartItemFromDb = await _db.ShoppingCart.Where(c => c.Id == cartId).FirstOrDefaultAsync();

            if (cartItemFromDb != null)
            {
                _db.ShoppingCart.Remove(cartItemFromDb);
                await _db.SaveChangesAsync();

                var cnt = _db.ShoppingCart.Where(c => c.ApplicationUserId == cartItemFromDb.ApplicationUserId).ToList().Count;
                HttpContext.Session.SetInt32(SD.ssShoppingCartCount, cnt);

            }
            return RedirectToAction(nameof(Index));
        }
    }
}