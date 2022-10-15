using Microsoft.AspNetCore.Mvc;
using ProiectMaster.DataAccess;
using ProiectMaster.Models.Entites;
using ProiectMaster.Models.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;

namespace ProiectMaster.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductService productService;
        protected readonly MagazinVirtualContext _db;

        public HomeController(IProductService productService, MagazinVirtualContext db)
        {
            this.productService = productService;
            this._db = db;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var products = productService.GetAllProducts();
            return View(products);
        }

        [HttpGet]
        [Route("Details/{id}")]
        public IActionResult Details(int id)
        {
            var product = productService.GetProduct(id);
            return View(product);
        }

        [HttpPost]
        [Route("Add/{id}")]
        public IActionResult Add(int id, int qty)
        {
            var products = this._db.Products;
            Dictionary<int, int> shopList;

            if (HttpContext.Session.Get<Dictionary<int, int>>(SessionHelper.ShoppingCart) == null)
                shopList = new Dictionary<int, int>();
            else
                shopList = HttpContext.Session.Get<Dictionary<int, int>>(SessionHelper.ShoppingCart);

            foreach(Product p in products)
            {
                if (p.Id == id && qty > 0)
                {
                    if (shopList.ContainsKey(p.Id) )
                    {
                        shopList[p.Id] = qty;
                    }
                    else
                    {
                        shopList.Add(p.Id, qty);
                    }
                }                   
            }


            HttpContext.Session.Set(SessionHelper.ShoppingCart, shopList);

            return RedirectToAction("Index", "Home", productService.GetAllProducts());
        }

        [ActionName("RemoveFromCart")]
        public IActionResult Remove(int id)
        {
            var shopList = HttpContext.Session.Get<Dictionary<int, int>>(SessionHelper.ShoppingCart);

            if (shopList == null)
                return RedirectToAction("Cart", "Home", productService.GetAllProducts());

            if (shopList.ContainsKey(id))
                shopList.Remove(id);

            HttpContext.Session.Set(SessionHelper.ShoppingCart, shopList);

            return RedirectToAction("Cart", "Home", productService.GetAllProducts());
        }

        public IActionResult Cart()
        {
            List < Product > cartProd = new List<Product>();
            var shopList = HttpContext.Session.Get<Dictionary<int, int>>(SessionHelper.ShoppingCart);
            if (shopList == null)
            {
                shopList = new Dictionary<int, int>();
            }
            var products = this._db.Products;

            foreach(Product p in products)
            {
                if (shopList.ContainsKey(p.Id))
                    cartProd.Add(p);
            }

            return View(cartProd);
        }

        public IActionResult Checkout()
        {
            List<Product> cartProd = new List<Product>();
            var shopList = HttpContext.Session.Get<Dictionary<int, int>>(SessionHelper.ShoppingCart);
            if (shopList == null)
            {
                shopList = new Dictionary<int, int>();
            }
            var products = this._db.Products;

            foreach (Product p in products)
            {
                if (shopList.ContainsKey(p.Id))
                    cartProd.Add(p);
            }

            return View(cartProd);
        }

        [HttpPost]
        [Route("save-order")]
        public IActionResult Save(string name, string email)
        {
            List<Product> cartProd = new List<Product>();
            var shopList = HttpContext.Session.Get<Dictionary<int, int>>(SessionHelper.ShoppingCart);
            if (shopList == null)
            {
                shopList = new Dictionary<int, int>();
            }
            var products = this._db.Products;
            var totalPrice = Convert.ToDecimal(0);

            foreach (Product p in products)
            {
                if (shopList.ContainsKey(p.Id))
                    cartProd.Add(p);
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format("<h1>Your order has arrived!</h1> <br/>" +
                "<p>Dear {0},</p> " +
                "<p>Here is your order: </p>", name));
            foreach (Product prod in cartProd)
            {
                sb.AppendFormat(" <br/>Name: {0} <br/> Quantity: {1} <br/> Price: {2}$ <br/>", 
                    prod.Name, shopList[prod.Id], prod.Price * shopList[prod.Id]);
                totalPrice += prod.Price * shopList[prod.Id];
            }
            sb.Append(string.Format("<br/> <b>Your total is: </b> <br>  {0}$ ", totalPrice));


            //create the mail message
            MailMessage mail = new MailMessage();

            //set the addresses
            mail.From = new MailAddress("testtest2412000@gmail.com");
            mail.To.Add(email);

            //set the content
            mail.Subject = "Your order has arrived!";
            mail.Body = sb.ToString();
            mail.IsBodyHtml = true;

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential("testtest2412000@gmail.com", "uqcovjwookhfsxdc")
            };

            smtp.Send(mail);

            return RedirectToAction("Index", "Home", productService.GetAllProducts());
        }


    }
}
