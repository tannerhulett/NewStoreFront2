using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NewStoreFront.DATA.EF.Models;

namespace NewStoreFront.UI.MVC.Controllers
{
[Authorize(Roles = "Admin")]
public class ProductsController : Controller
    {
        private readonly NewStoreFrontContext _context;

        public ProductsController(NewStoreFrontContext context)
        {
            _context = context;
        }

        // GET: Products
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var newStoreFrontContext = _context.Products.Include(p => p.Category).Include(p => p.Supplier);
            return View(await newStoreFrontContext.ToListAsync());
        }

        // GET: Products/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Products == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [AllowAnonymous]
        public async Task<IActionResult> TiledProducts(string searchTerm, int categoryId = 0, int page = 1)
        {
            int pageSize = 6;

            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Include(p => p.OrderProducts).ToList();

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName");

            ViewBag.Category = 0;

            if (categoryId != 0)
            {
                products = products.Where(p => p.CategoryId == categoryId).ToList();

                //Repopulate the dropdown with current category selected
                ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "ProductType", categoryId);

                ViewBag.Category = categoryId;
            }

            if (!String.IsNullOrEmpty(searchTerm))
            {
                products = products.Where(p =>
                    p.ProductName.ToLower().Contains(searchTerm.ToLower()) ||
                    p.Supplier.SupplierName.ToLower().Contains(searchTerm.ToLower()) ||
                    p.Category.CategoryName.ToLower().Contains(searchTerm.ToLower()) ||
                    p.ProductDescription.ToLower().Contains(searchTerm.ToLower())).ToList();

                //Viewbag for the total number of results
                ViewBag.NbrResults = products.Count();

                //Viewbag for the search term
                ViewBag.SearchTerm = searchTerm;

            }
            else
            {
                ViewBag.NbrResults = null;
                ViewBag.SearchTerm = null;
            }

            return View(products);

        }

        // GET: Products/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName");
            ViewData["SupplierId"] = new SelectList(_context.Suppliers, "SupplierId", "Address");
            return View();
        }

        // POST: Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,ProductName,ProductPrice,ProductDescription,UnitsInStock,UnitsOnOrder,CategoryId,SupplierId,ProductImage")] Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", product.CategoryId);
            ViewData["SupplierId"] = new SelectList(_context.Suppliers, "SupplierId", "Address", product.SupplierId);
            return View(product);
        }

        // GET: Products/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Products == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", product.CategoryId);
            ViewData["SupplierId"] = new SelectList(_context.Suppliers, "SupplierId", "Address", product.SupplierId);
            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,ProductName,ProductPrice,ProductDescription,UnitsInStock,UnitsOnOrder,CategoryId,SupplierId,ProductImage")] Product product)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", product.CategoryId);
            ViewData["SupplierId"] = new SelectList(_context.Suppliers, "SupplierId", "Address", product.SupplierId);
            return View(product);
        }

        // GET: Products/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Products == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Products == null)
            {
                return Problem("Entity set 'NewStoreFrontContext.Products'  is null.");
            }
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
          return (_context.Products?.Any(e => e.ProductId == id)).GetValueOrDefault();
        }
    }
}
