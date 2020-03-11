using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;

namespace Spice.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class MenuItemController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _hostingEnvironment;

        [BindProperty]
        public MenuItemViewModel MenuItemVM { get; set; }

        public MenuItemController(ApplicationDbContext db, IWebHostEnvironment hostingEnvironment)
        {
            _db = db;
            _hostingEnvironment = hostingEnvironment;
            MenuItemVM = new MenuItemViewModel()
            {
                Category = _db.Category,
                MenuItem = new MenuItem()
            };
        }


        // GET
        public async Task<IActionResult> Index()
        {
            var MenuItems = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory).ToListAsync();
            return View(MenuItems);
        }

        // GET - CREATE
        public IActionResult Create()
        {
            return View(MenuItemVM);
        }
    }
}