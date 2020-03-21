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
using Stripe;
using Microsoft.EntityFrameworkCore.Storage;

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

            foreach (var item in DetailsCart.ListCart)
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

        [HttpPost]
        [ActionName("Summary")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SummaryConfirmed(string stripeToken)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            DetailsCart.ListCart = await _db.ShoppingCart.Where(c => c.ApplicationUserId == claim.Value).ToListAsync();

            DetailsCart.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            DetailsCart.OrderHeader.OrderDate = DateTime.Now;
            DetailsCart.OrderHeader.UserId = claim.Value;
            DetailsCart.OrderHeader.Status = SD.PaymentStatusPending;
            DetailsCart.OrderHeader.PickupTime = Convert.ToDateTime(DetailsCart.OrderHeader.PickupDate.ToShortDateString() + " " + DetailsCart.OrderHeader.PickupTime.ToShortTimeString());

            using (IDbContextTransaction transaction = _db.Database.BeginTransaction())
            {
                List<OrderDetails> orderDetailsList = new List<OrderDetails>();
                _db.OrderHeader.Add(DetailsCart.OrderHeader);
                await _db.SaveChangesAsync();

                DetailsCart.OrderHeader.OrderTotalOriginal = 0;

                foreach (var item in DetailsCart.ListCart)
                {
                    item.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == item.MenuItemId);
                    OrderDetails orderDetails = new OrderDetails()
                    {
                        MenuItemId = item.MenuItem.Id,
                        OrderId = DetailsCart.OrderHeader.Id,
                        Description = item.MenuItem.Description,
                        Name = item.MenuItem.Name,
                        Price = item.MenuItem.Price,
                        Count = item.Count
                    };
                    DetailsCart.OrderHeader.OrderTotalOriginal += orderDetails.Count * orderDetails.Price;
                    _db.OrderDetails.Add(orderDetails);
                }

                if (HttpContext.Session.GetString(SD.ssCouponCode) != null)
                {
                    DetailsCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
                    var couponFromDb = await _db.Coupon.FirstOrDefaultAsync(c => c.Name.ToLower().Trim() == DetailsCart.OrderHeader.CouponCode.ToLower().Trim());
                    DetailsCart.OrderHeader.OrderTotal = SD.DiscountedPrice(couponFromDb, DetailsCart.OrderHeader.OrderTotalOriginal);
                }
                else
                {
                    DetailsCart.OrderHeader.OrderTotal = DetailsCart.OrderHeader.OrderTotalOriginal;
                }

                DetailsCart.OrderHeader.CouponDiscount = DetailsCart.OrderHeader.OrderTotalOriginal - DetailsCart.OrderHeader.OrderTotal;

                // Remove Cart Items from database
                _db.ShoppingCart.RemoveRange(DetailsCart.ListCart);

                try
                {
                    var options = new ChargeCreateOptions
                    {
                        Amount = Convert.ToInt32(DetailsCart.OrderHeader.OrderTotal * 100),
                        Currency = "usd",
                        Description = "Order ID : " + DetailsCart.OrderHeader.Id.ToString(),
                        Source = stripeToken
                    };

                    var service = new ChargeService();
                    Charge charge = service.Create(options);

                    if (charge.BalanceTransactionId == null)
                    {
                        DetailsCart.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
                    }
                    else
                    {
                        DetailsCart.OrderHeader.TransactionId = charge.BalanceTransactionId;
                    }

                    if (charge.Status.ToLower() == "succeeded")
                    {
                        DetailsCart.OrderHeader.PaymentStatus = SD.PaymentStatusApproved;
                        DetailsCart.OrderHeader.Status = SD.StatusSubmitted;
                    }
                    else
                    {
                        DetailsCart.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
                    }

                    await _db.SaveChangesAsync();
                    transaction.Commit();

                    // Remove session ssShoppingCartCount & ssCouponCode
                    HttpContext.Session.SetInt32(SD.ssShoppingCartCount, 0);
                    HttpContext.Session.SetString(SD.ssCouponCode, string.Empty);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    NotFound(ex.Message);
                    View(DetailsCart);
                }
            }
            return RedirectToAction("Index", "Home");
            //return RedirectToAction("Confirm", "Order", new { DetailsCart.OrderHeader.Id });
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