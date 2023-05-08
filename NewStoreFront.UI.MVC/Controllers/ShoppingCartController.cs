using Microsoft.AspNetCore.Mvc;
using NewStoreFront.DATA.EF.Models; //Grants access to GadgetStoreContext and Product classes
using Microsoft.AspNetCore.Identity; //Grants access to Identity classes & methods
using NewStoreFront.UI.MVC.Models; //Grants access to CartItemViewModel class
using Newtonsoft.Json; //Grants access to JSON classes & methonds (Serialization & Deserialization)
using System.Security.Cryptography.X509Certificates;
using System.Drawing;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.AspNetCore.Authorization;
using NewStoreFront.DATA.EF.Models;
using NewStoreFront.UI.MVC.Models;

namespace NewStoreFront.UI.MVC.Controllers
{
    public class ShoppingCartController : Controller
    {



        //Fields/Props
        private readonly NewStoreFrontContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ShoppingCartController(NewStoreFrontContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {

            //Retrieve the contents from the Session shopping cart (stored as JSON) and convert them to C# via Newtonsoft.Json. After converting to C#, we can pass the collection of cart contents back to the strongly-typed view to display.

            //Retrive the cart contents
            var sessionCart = HttpContext.Session.GetString("cart");

            //Create a shell for the local version (C# Cart)
            Dictionary<int, CartItemViewModel> cart = null;

            //Check to see if teh session cart was null
            if (sessionCart == null || sessionCart.Count() == 0)
            {
                //User either hasn't put anything in the cart or they have removed all items.
                //So, set the shoppingCart to an empty object
                cart = new Dictionary<int, CartItemViewModel>();

                ViewBag.Message = "There are no items in your cart.";
            }
            else
            {
                ViewBag.Message = null;

                //Deserialize the cart contents from JSON to C#
                cart = JsonConvert.DeserializeObject<Dictionary<int, CartItemViewModel>>(sessionCart);
            }

            return View(cart);
        }

        public IActionResult AddToCart(int id)
        {
            //Create an empty shell for the LOCAL shopping cart variable (NOT the cart in the session)
            //int (key) => ProductId
            //CartItemViewModel (value) => Product & Qty
            Dictionary<int, CartItemViewModel> cart = null;


            var sessionCart = HttpContext.Session.GetString("cart");

            //Check to see if the Session object exists
            //If not, instantiate a new local collection
            if (String.IsNullOrEmpty(sessionCart))
            {
                //Static vs Instance Methods
                //Static > Called from the CLASS
                //Instance > Called from a specific object (an INSTANCE of that class)
                //string name = "Evan";

                //name.ToUpper();

                //string.IsNullOrEmpty(name);

                //If the session didn't exist yet, instantiate the collection as a new object
                cart = new Dictionary<int, CartItemViewModel>();

            }
            else
            {
                //Cart already exists -- transfer the existing cart items from the Session into our local shoppinCart

                //DeserializeObject() is a methos that converts JSON to C#. We MUST tell this method
                //which C# class to convert the JSON into (here -- Dictionary<int, CIVM>)
                cart = JsonConvert.DeserializeObject<Dictionary<int, CartItemViewModel>>(sessionCart);

            }

            //Add the newly selected product to the cart
            //Retrive the Product from the Database
            Product product = _context.Products.Find(id);

            //Instantiate the CIVM object so we can add it to the cart
            CartItemViewModel civm = new CartItemViewModel(1, product); //Adds 1 of the selected product to the cart

            //If the product was already in the cart, increase quantity by 1
            //Otherwise, just add the new item to the cart
            if (cart.ContainsKey(product.ProductId))
            {
                //Update the Qty
                cart[product.ProductId].Qty++;
            }
            else
            {
                //Add the item to the cart
                cart.Add(product.ProductId, civm);
            }

            //Update the session the version of the cart
            //Take the local version and serialize it as JSON
            string jsonCart = JsonConvert.SerializeObject(cart);

            //Assign that value to our Session
            HttpContext.Session.SetString("cart", jsonCart);

            return RedirectToAction("Index");

        }

        public IActionResult RemoveFromCart(int id)
        {
            //Retrieve the cart from the session
            var sessionCart = HttpContext.Session.GetString("cart");

            //Convert JSON to C#
            Dictionary<int, CartItemViewModel> cart = JsonConvert.DeserializeObject<Dictionary<int, CartItemViewModel>>(sessionCart);

            //Remove cart item
            cart.Remove(id);

            //If there are no remaining items in the cart, remove it from the session
            if (cart.Count == 0)
            {
                HttpContext.Session.Remove("cart");
            }
            //Otherwise, update the session variable with the new local cart contents
            else
            {
                string jsonCart = JsonConvert.SerializeObject(cart);
                HttpContext.Session.SetString("cart", jsonCart);
            }

            return RedirectToAction("Index");

        }

        public IActionResult UpdateCart(int productid, int qty)
        {
            //Retrieve the cart from the Session
            var sessionCart = HttpContext.Session.GetString("cart");

            //Deserialize from JSON to C#
            Dictionary<int, CartItemViewModel> cart = JsonConvert.DeserializeObject<Dictionary<int, CartItemViewModel>>(sessionCart);

            //Update the qty from our specific dictionary key
            cart[productid].Qty = qty;

            //Update the session
            string jsonCart = JsonConvert.SerializeObject(cart);
            HttpContext.Session.SetString("cart", jsonCart);

            return RedirectToAction("Index");
        }

        //Submit button
        [Authorize]
        public async Task<IActionResult> SubmitOrder()
        {

            string? userId = (await _userManager.GetUserAsync(HttpContext.User))?.Id;

            //Retrieve the UserDetails record
            UserDetail ud = _context.UserDetails.Find(userId);

            //Create the order object
            Order o = new()
            {
                OrderDate = DateTime.Now,
                UserId = userId,
                ShipCity = ud.City,
                ShipToName = ud.FirstName + " " + ud.LastName,
                ShipState = ud.State,
                ShipZip = ud.Zip
            };

            //Add the Order to _context
            _context.Orders.Add(o);

            //retrieve the session shopping cart
            var sessionCart = HttpContext.Session.GetString("cart");

            //Convery to C#
            Dictionary<int, CartItemViewModel> cart = JsonConvert.DeserializeObject<Dictionary<int, CartItemViewModel>>(sessionCart);

            //Create an OrderProduct object for each item in the cart
            foreach (var item in cart)
            {
                OrderProduct op = new OrderProduct()
                {
                    ProductId = item.Key,
                    OrderId = o.OrderId,
                    Quantity = (short?)item.Value.Qty,
                    ProductPrice = item.Value.Product.ProductPrice,
                };

                //For linking table records, we can add items/records to an existing entity
                //if the records are related >
                o.OrderProducts.Add(op);

                //remove the item from the cart
                cart.Remove(item.Key);
            }

            //Save changes to db
            _context.SaveChanges();

            //Empty the cart/Remove the cart strip from the session
            HttpContext.Session.Remove("cart");


            //Redirect the user to the orders index
            return RedirectToAction("Index", "Orders");
        }

    }
}
