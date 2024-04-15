using System;
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
using Newtonsoft.Json;

namespace CoinpayNFT.Controllers
{
    public class AuctionController : Controller
    {
        public ActionResult Home()
        {
            return View();
        }
        [Authorize]
        public ActionResult CreateNft()
        {
            return View();
        }
        public ActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.address = ((FormsIdentity)User.Identity).Ticket.GetStructuredUserData();
            }
            return View();
        }
        public ActionResult ViewDetail(int auctionId)
        {
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.address = ((FormsIdentity)User.Identity).Ticket.GetStructuredUserData();
            }
            return View();
        }
        [HttpPost]//optional
        public async Task<ActionResult> CreateNft(HttpPostedFileBase file1,Nft nft, HttpPostedFileBase file2)
        {
            var hash = await PinataService.GetHashAsync(new StreamContent(file1.InputStream), file1.FileName);
            nft.hash = hash;
            //var v= await PinataService.UnPinAsync(hash);
            var payload = new 
            {
                name=nft.name,
                description=nft.description,
                image="ipfs://"+hash+"/"+file1.FileName
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
     


     

        public ActionResult Help()
        {
            return View();
        }
        public ActionResult Contact()
        {
            return View();
        }
        public ActionResult VoteDao()
        {
            return View();
        }
        public ActionResult Proposals()
        {
            return View();
        }
    }
}