﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using CoinpayNFT.Models;
using CoinpayNFT.services;
using FormsAuthenticationExtensions;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;

namespace CoinpayNFT.Controllers
{
    public class HomeController : Controller
    {
        //[Authorize]
        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> Index(HttpPostedFileBase file1,Nft nft, HttpPostedFileBase file2)
        {
            var hash = await PinataService.GetHashAsync(new StreamContent(file1.InputStream), file1.FileName);
            nft.hash = hash;
            //var v= await PinataService.UnPinAsync(hash);
            var payload = new 
            {
                name=nft.name,
                description=nft.description,
                image="ipfs://"+hash+"/"+file1.FileName,
                author=User.Identity.Name.Split('@')[0]
            };
            nft.image = payload.image;
            var stringPayload = JsonConvert.SerializeObject(payload);
            var httpContent = new StringContent(stringPayload, Encoding.UTF8, "application/json");
            hash = await PinataService.GetHashAsync(httpContent, "metadata.json");
            nft.metadata = hash + "/metadata.json";//already prefixed "ipfs://" + 

            if (file2 != null)
            {
                 hash = await PinataService.GetHashAsync(new StreamContent(file2.InputStream), file2.FileName);
                nft.secretLink = "ipfs://" + hash + "/"+ file2.FileName;
            }
            nft.email = User.Identity.Name;
            Nftcrud.nftList.Add(nft);
            return RedirectToAction("Inventory");
        }
        [Authorize]
        public ActionResult Index2()
        {
            ViewBag.address = ((FormsIdentity)User.Identity).Ticket.GetStructuredUserData();
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> Index2(HttpPostedFileBase file1, Nft nft)
        {
            var hash = await PinataService.GetHashAsync(new StreamContent(file1.InputStream), file1.FileName);
            ViewBag.hash = hash;
            //var v= await PinataService.UnPinAsync(hash);
            var payload = new
            {
                name = nft.name,
                description = nft.description,
                image = "ipfs://" + hash + "/" + file1.FileName
            };
            nft.image = payload.image;
            var stringPayload = JsonConvert.SerializeObject(payload);
            var httpContent = new StringContent(stringPayload, Encoding.UTF8, "application/json");
            hash = await PinataService.GetHashAsync(httpContent, "metadata.json");
            ViewBag.metadata = hash + "/metadata.json";//already prefixed "ipfs://" + 

            ViewBag.address = ((FormsIdentity)User.Identity).Ticket.GetStructuredUserData();
            return View();
        }
        public ActionResult Auctions()
        {
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.address = ((FormsIdentity)User.Identity).Ticket.GetStructuredUserData();
            }
            return View();
        }

        public ActionResult Inventory()
        {
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.address = ((FormsIdentity)User.Identity).Ticket.GetStructuredUserData();
            }
                
            return View(Nftcrud.nftList.Where(x => x.email == User.Identity.Name).ToList());
        }
        public ActionResult NftAdminSingle(Guid id)
        {
            return View(Nftcrud.nftList.Where(x => x.id == id));
        }
        public ActionResult NftAdmin()
        {
            return View(Nftcrud.nftList.Where(x=>x.status== "Reviewing").ToList());
        }
        public ActionResult NftStatus(string status,Guid id)
        {
            var nft = Nftcrud.nftList.First(x => x.id == id);
            nft.status = status;
            return RedirectToAction("NftAdmin");
        }
        public async Task<ActionResult> NftDelete(Guid id,bool isPublish=false)
        {
            var nft = Nftcrud.nftList.First(x => x.id == id);
            if (isPublish == false)
            {
                await PinataService.UnPinAsync(nft.hash);
                await PinataService.UnPinAsync(nft.metadata.Replace("ipfs://", "").Split('/')[0]);
                if (!string.IsNullOrEmpty(nft.secretLink))
                {
                    await PinataService.UnPinAsync(nft.secretLink.Replace("ipfs://", "").Split('/')[0]);
                }
            }
            
            Nftcrud.nftList.Remove(nft);
            return RedirectToAction("Inventory");
        }


        private string ModifyPass(string password)
        {
            //email = HttpUtility.UrlEncode(email);
            //password = HttpUtility.UrlEncode(password);
            string str = "";
            foreach (char c in password)
            {
                if (c == '#')
                {
                    str += "%23";
                }
                else if (c == '%')
                {
                    str += "%25";
                }
                else if (c == '&')
                {
                    str += "%26";
                }
                else
                {
                    str += c;
                }
            }
            return str;
        }

        public ActionResult Login(string email, string ethAddress)
        {
            var addr = new Nethereum.Util.AddressUtil();

            if (addr.IsValidEthereumAddressHexFormat(ethAddress))
            {
                //FormsAuthenticationTicket authTicket = new
                //FormsAuthenticationTicket(1, //version
                //                          email, // user name
                //                          DateTime.Now,             //creation
                //                          DateTime.Now.AddHours(24), //Expiration
                //                          true, ethAddress); //storing the json data

                //string encTicket = FormsAuthentication.Encrypt(authTicket);

                //var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encTicket)
                //{
                //    Expires = authTicket.Expiration,
                //    Path = FormsAuthentication.FormsCookiePath
                //};

                //if (HttpContext != null)
                //{
                //    HttpContext.Response.Cookies.Add(cookie);
                //}
                //var ticketData
                new FormsAuthentication().SetAuthCookie(email, true, ethAddress);
                return RedirectToAction("Index");
            }

            //TempData["msg"] = ethAddress;

            return Content(ethAddress);
        }

        //public ActionResult Login()
        //{
        //    return View();
        //}
        [HttpPost]
        public ActionResult Login(string email, string password, string chatid = "")
        {
            //api
            WebClient syncClient = new WebClient();
            string content = "";
            try
            {
                password = ModifyPass(password);

                content = syncClient.DownloadString($"https://coinpay.cr/MLMApi/api/MLM/Login?username={email}&password={password}");

            }
            catch (Exception ex)
            {
                TempData["msg"] = "Invalid login attempt." + ex.Message;
                return View();
            }
            content = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(content);
            //content = "0x45BD27bB68B07FE9541B26955d880b70df34B960";

            var addr = new Nethereum.Util.AddressUtil();

            if (addr.IsValidEthereumAddressHexFormat(content))
            {
                FormsAuthenticationTicket authTicket = new
                FormsAuthenticationTicket(1, //version
                                          email, // user name
                                          DateTime.Now,             //creation
                                          DateTime.Now.AddDays(24), //Expiration
                                          true, content); //storing the json data

                string encTicket = FormsAuthentication.Encrypt(authTicket);

                var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encTicket)
                {
                    Expires = authTicket.Expiration,
                    Path = FormsAuthentication.FormsCookiePath
                };

                if (HttpContext != null)
                {
                    HttpContext.Response.Cookies.Add(cookie);
                }
                return RedirectToAction("Index");
            }

            TempData["msg"] = content;
            return View();


        }
        //[Authorize]
        public ActionResult Logout()
        {
            var userAddress = ((FormsIdentity)User.Identity).Ticket.GetStructuredUserData();

            FormsAuthentication.SignOut();
            return RedirectToAction("Index","Auction");
        }

        public string GetTxId(decimal price, string addressTo, string data = "0x")
        {
            HttpCookie authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(authCookie.Value);

            string ethAddr = ticket.UserData;
            string email = User.Identity.Name;
            string url = $"https://coinpay.cr/MLMApi/api/MLM/getTrnxInfo?username={email}&password=&addressFrom={ethAddr}&addressTo={addressTo}&data={data}&price={price}";



            try
            {
                //api
                WebClient syncClient = new WebClient();
                string content = syncClient.DownloadString(url);
                content = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(content);
                return content;
            }
            catch (Exception ex)
            {
                //email
                //await SendEmail.SendEmailAsync("toufiqelahy@hotmail.com", url + " Exception:" + ex.Message);
                return "Exception." + ex.Message;
            }

        }
        public ActionResult Profile()
        {
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.address = ((FormsIdentity)User.Identity).Ticket.GetStructuredUserData();
            }
            return View();
        }
        public ActionResult Message()
        {
            return View();
        }
        public ActionResult Earnings()
        {
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.address = ((FormsIdentity)User.Identity).Ticket.GetStructuredUserData();
            }
            return View();
        }
        public ActionResult MyOrders()
        {
            return View();
        }
        public ActionResult BiddingHistory()
        {
            return View();
        }
        public ActionResult WonAuctions()
        {
            return View();
        }
        public ActionResult Reviews()
        {
            return View();
        }
        public ActionResult Listing()
        {
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.address = ((FormsIdentity)User.Identity).Ticket.GetStructuredUserData();
            }
            return View();
        }
        public ActionResult CurrentItemList()
        {
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.address = ((FormsIdentity)User.Identity).Ticket.GetStructuredUserData();
            }
            return View();
        }
        public ActionResult SoldItemList()
        {
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.address = ((FormsIdentity)User.Identity).Ticket.GetStructuredUserData();
            }
            return View();
        }
        public ActionResult History()
        {
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.address = ((FormsIdentity)User.Identity).Ticket.GetStructuredUserData();
            }
            return View();
        }
        public async Task<string> NftApi(string data, decimal amount = 0)

        {
            var client = new HttpClient();
            var email = User.Identity.Name;
            string contractAddress = "0xd8eB48885429600dc036D10D1e61b503c5AF7e44";
            var content = new StringContent(JsonConvert.SerializeObject(new { email, amount, contractAddress, data }));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            var response = await client.PostAsync("https://api.coinpay.cr/api/client/nft/apicall/v1", content);

            var value = await response.Content.ReadAsStringAsync();

            var txId = JsonConvert.DeserializeObject<string>(value);
            return txId;
        }

            //https://api.coinpay.cr/api/client/nft/apicall/{email}/{amount}/{contractAddress}/{data}/v1
            //amount in wei
            ////https://api.coinpay.cr/api/client/nft/apicall/toufiqelahy@hotmail.com/0/0xd8eB48885429600dc036D10D1e61b503c5AF7e44/0xdd1ac4be00000000000000000000000000000000000000000000000000000000000000e00000000000000000000000000000000000000000000000000000000000000120000000000000000000000000000000000000000000000000000000000000018000000000000000000000000000000000000000000000000000000000000001a000000000000000000000000000000000000000000000000000071afd498d0000000000000000000000000000000000000000000000000000000020bde7364000000000000000000000000000bcb746e20d370cc82c3d56661eb649278af3382c000000000000000000000000000000000000000000000000000000000000000b7265616c20657374617465000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000002e516d59415573637038674a6e737133434c575a70544d62356d7a51437a41317171387a4c7864476f6e6b757a39370000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000003c516d63375779626362683261316e3477504c4476335a6d3773554e5862564667316d444d5153794b7878653537482f6d657461646174612e6a736f6e00000000/v1
            /////
            //var privatevKey = "f92307abd3dfc5eeaa00cf856ed34dbd7c84e1bd01a6a7cb5094e69845f2b298";//private key by user email
            //var account = new Account(privatevKey, 15001);
            //var web3 = new Web3(account,"https://blockchain.coinpay.cr");
            //var cp = new CallInput();
            //cp.Data = data;
            //cp.From = senderAddress;
            //cp.To = contractAddress;
            //cp.Value = new Nethereum.Hex.HexTypes.HexBigInteger(amount);

            //TransactionInput tp = new TransactionInput();
            //tp.Data = data;
            //tp.From = senderAddress;
            //try
            //{
            //    tp.Gas = await web3.Eth.TransactionManager.EstimateGasAsync(cp);
            //    tp.GasPrice = await web3.Eth.GasPrice.SendRequestAsync();
            //}
            //catch (Exception ex)
            //{

            //    throw ex;
            //}
            ////tp.Nonce = txCount;
            //tp.To = contractAddress;
            //tp.Value = new Nethereum.Hex.HexTypes.HexBigInteger(amount);
            //var txId = await web3.Eth.TransactionManager.SendTransactionAsync(tp);

            //return txId;
        
    }
}