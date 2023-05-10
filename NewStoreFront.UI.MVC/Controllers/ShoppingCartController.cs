using Microsoft.AspNetCore.Mvc;

using NewStoreFront.DATA.EF.Models; //Grants access to GadgetStoreContext and Product classes
using Microsoft.AspNetCore.Identity; //Grants access to Identity classes & methods
using NewStoreFront.UI.MVC.Models; //Grants access to CartItemViewModel class
using Newtonsoft.Json; //Grants access to JSON classes & methods (Serialization & Deserialization)


namespace NewStoreFront.UI.MVC.Controllers
{
    public class ShoppingCartController : Controller
    {
        #region Steps to Implement Session Based Shopping Cart
        /*
         * 1) Register Session in program.cs (builder.Services.AddSession... && app.UseSession())
         * 2) Create the CartItemViewModel class in [ProjName].UI.MVC/Models folder
         * 3) Add the 'Add To Cart' button in the Index and/or Details view of your Products
         * 4) Create the ShoppingCartController (empty controller -> named ShoppingCartController)
         *      - add using statements
         *          - using GadgetStore.DATA.EF.Models;
         *          - using Microsoft.AspNetCore.Identity;
         *          - using GadgetStore.UI.MVC.Models;
         *          - using Newtonsoft.Json;
         *      - Add props/fields for the GadgetStoreContext && UserManager
         *      - Create a constructor for the controller - assign values to context && usermanager
         *      - Code the AddToCart() action
         *      - Code the Index() action
         *      - Code the Index View
         *          - Start with the basic table structure
         *          - Show the items that are easily accessible (like the properties from the model)
         *          - Calculate/show the lineTotal
         *          - Add the RemoveFromCart <a>
         *      - Code the RemoveFromCart() action
         *          - verify the button for RemoveFromCart in the Index view is coded with the controller/action/id
         *      - Add UpdateCart <form> to the Index View
         *      - Code the UpdateCart() action
         *      - Add Submit Order button to Index View
         *      - Code SubmitOrder() action
         * */
        #endregion

        //Fields/Props
        private readonly NewStoreFrontContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        //Constructor
        public ShoppingCartController(NewStoreFrontContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            //Retrieve the contents from the Session shopping cart (stored as JSON) and 
            //convert them to C# via Newtonsoft.Json. After converting to c#, we can pass 
            //the collection of cart contents back to the strongly-typed view to display.

            //Retrieve the cart contents
            var sessionCart = HttpContext.Session.GetString("cart");

            //Create a shell for the local version (C# cart)
            Dictionary<int, CartItemViewModel> shoppingCart = null;

            //Check to see if the session cart was null
            if (sessionCart == null || sessionCart.Count() == 0)
            {
                //User either hasn't put anything in the cart or they have removed all items.
                //So, set the shoppingCart to an empty object
                shoppingCart = new Dictionary<int, CartItemViewModel>();

                ViewBag.Message = "There are no items in your cart.";
            }
            else
            {
                ViewBag.Message = null;

                //Deserialize the cart contents from JSON to C#
                shoppingCart = JsonConvert.DeserializeObject<Dictionary<int, CartItemViewModel>>(sessionCart);
            }

            return View(shoppingCart);
        }


        public IActionResult AddToCart(int id)
        {
            //Create an empty shell for the LOCAL shopping cart variable (NOT the cart in the session)
            //int (key) => ProductId
            //CartItemViewModel (value) => Product & Qty
            Dictionary<int, CartItemViewModel> shoppingCart = null;

            #region Session Notes

            /*
             * Session is a tool available on the server-side that can store information for a user while 
             * they are actively using your site.
             * 
             * Typically, the session lasts for 20 minutes (this can be adjusted in Program.cs).
             * Once the 20 minutes is up, the session variable is disposed.
             * 
             * Values that we can store in the session are limited to: string, int
             *  - Because of this limitation, we have to get creative when trying to store complex objects 
             *  (like CartItemViewModel objects). 
             *  - To keep the info separated into properties, we will convert the C# object into a JSON string
             */

            #endregion

            //Create a variable to store the info/contents of the Session cart
            var sessionCart = HttpContext.Session.GetString("cart");

            //Check to see if the Session object exists
            //If not, instantiate a new local collection 
            if (String.IsNullOrEmpty(sessionCart))
            {
                //Static vs Instance Methods
                //Static > Called from the CLASS
                //Instance > Called from a specific object (an INSTANCE of that class)

                //If the session didn't exist yet, instantiate the collection as a new object
                shoppingCart = new Dictionary<int, CartItemViewModel>();

            }
            else
            {
                //Cart already exists -- transfer the existing cart items from the Session into 
                //our local shoppingCart.

                //DeserializeObject() is a method that converts JSON to C#. We MUST tell this method 
                //which C# class to convert the JSON into (here -- Dictionary<int, CIVM>)
                shoppingCart = JsonConvert.DeserializeObject<Dictionary<int, CartItemViewModel>>(sessionCart);

            }


            //Add the newly selected product to the cart
            //Retrieve the Product from the Database
            Product product = _context.Products.Find(id);

            //Instantiate the CIVM object so we can add it to the cart
            CartItemViewModel civm = new CartItemViewModel(1, product); //Adds 1 of the selected product to the cart

            //If the product was already in the cart, increase the quantity by 1
            //Otherwise, just add the new item to the cart
            if (shoppingCart.ContainsKey(product.ProductId))
            {
                //Update the Qty
                shoppingCart[product.ProductId].Qty++;
            }
            else
            {
                //Add the item to the cart
                shoppingCart.Add(product.ProductId, civm);
            }

            //Update the session the version of the cart
            //Take the local version and serialize it as JSON
            string jsonCart = JsonConvert.SerializeObject(shoppingCart);

            //Assign that value to our Session
            HttpContext.Session.SetString("cart", jsonCart);

            return RedirectToAction("Index");
        }


        public IActionResult RemoveFromCart(int id)
        {
            //Retrieve the cart from the session
            var sessionCart = HttpContext.Session.GetString("cart");

            //Convert JSON to C#
            Dictionary<int, CartItemViewModel> shoppingCart = JsonConvert.DeserializeObject<Dictionary<int, CartItemViewModel>>(sessionCart);

            //Remove cart item
            shoppingCart.Remove(id);

            //If there are no remaining items in the cart, remove it from the session
            if (shoppingCart.Count == 0)
            {
                HttpContext.Session.Remove("cart");
            }
            //Otherwise, update the session variable with the new local cart contents
            else
            {
                string jsonCart = JsonConvert.SerializeObject(shoppingCart);
                HttpContext.Session.SetString("cart", jsonCart);
            }

            return RedirectToAction("Index");

        }


        public IActionResult UpdateCart(int productId, int qty)
        {
            //Retrieve the cart from the Session
            var sessionCart = HttpContext.Session.GetString("cart");

            //Deserialize from JSON to C#
            Dictionary<int, CartItemViewModel> shoppingCart = JsonConvert.DeserializeObject<Dictionary<int, CartItemViewModel>>(sessionCart);

            //Update the qty from our specific dictionary key
            shoppingCart[productId].Qty = qty;

            //Update the session
            string jsonCart = JsonConvert.SerializeObject(shoppingCart);
            HttpContext.Session.SetString("cart", jsonCart);

            return RedirectToAction("Index");
        }


        public async Task<IActionResult> SubmitOrder()
        {
            #region Planning Out Order Submission

            //The Big Picture:
            //Create an Order object when the user submits and save it to the database
            // - OrderDate (supplied programmatically) 
            // - UserId (supplied by the active user)
            // - ShipToName, ShipCity, ShipState, ShipZip > This info will be pulled from 
            //      the UserDetails table by looking up the record for the current UserId
            //Add the record to the _context (queue it up to be added in the DB)
            //Save the database changes

            //Create an OrderProduct object for each item in the cart
            //  - ProductId > Pulled from the cart
            //  - OrderId > Pulled from the Order object
            //  - Qty > Pulled from the cart
            //  - ProductPrice > Available from the cart
            //Add the record to the _context
            //Save the database changes


            #endregion

            //Retrieve the current user's Id
            //The code below is a mechanism provided by Identity to retrieve the UserID in the 
            //current HttpContext. If you need to retrieve the UserId in any controller, you 
            //can use this.
            string? userId = (await _userManager.GetUserAsync(HttpContext.User))?.Id;

            //Retrieve the UserDetails record
            UserDetail ud = _context.UserDetails.Find(userId);

            //Create the order object
            Order o = new Order()
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


            //Retrieve the session shopping cart
            var sessionCart = HttpContext.Session.GetString("cart");

            //Convert it to C#
            Dictionary<int, CartItemViewModel> shoppingCart = JsonConvert.DeserializeObject<Dictionary<int, CartItemViewModel>>(sessionCart);

            //Create an OrderProduct object for each item in the cart
            foreach (var item in shoppingCart)
            {
                OrderProduct op = new OrderProduct()
                {
                    ProductId = item.Key,
                    OrderId = o.OrderId,
                    Quantity = (short)item.Value.Qty,
                    ProductPrice = item.Value.Product.ProductPrice
                };

                //For linking table records, we can add items/records to an existing entity 
                //if the records are related.
                o.OrderProducts.Add(op);

                //Remove the item from the cart
                //shoppingCart.Remove(item.Key);
            }

            //Save the changes to the database
            _context.SaveChanges();


            //Removing the cart string from the session
            HttpContext.Session.Remove("cart");


            //Redirect the user to the Orders Index
            return RedirectToAction("Index", "Orders");

        }

    }
}
