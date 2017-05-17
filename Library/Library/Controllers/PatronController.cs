using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Library.Repository;
using Microsoft.Extensions.Configuration;
using Library.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Library.Controllers
{
    [Route("api/[controller]")]
    public class PatronController : Controller
    {
        private readonly PatronRepository patronRepository;

        public PatronController(IConfiguration configuration)
        {
            patronRepository = new PatronRepository(configuration);
        }

        [HttpGet]
        public IEnumerable<Patron> GetAll()
        {
            return patronRepository.FindAll();
        }

        [HttpGet("{idPatron}", Name = "GetPatron")]
        public IActionResult GetById(long idPatron)
        {
            var item = patronRepository.FindByID(idPatron);
            if (item == null)
            {
                return NotFound("Patron with id : " + idPatron + " is not present. Enter valid patron id.");
            }
            return Ok(item);
        }

        [HttpPost]
        public IActionResult Create([FromBody] Patron item)
        {
            if (item == null)
            {
                return BadRequest();
            }
            Patron lastItem = patronRepository.Add(item);
            if (lastItem == null)
            {
                return StatusCode(500, "Could not add last item successfully. Please try again.");
            }
            long newID = lastItem.patronid;
            return CreatedAtRoute("GetPatron", new { idPatron = newID }, lastItem);
        }

        [HttpPut("{idPatron}")]
        public IActionResult Update(long idPatron, [FromBody] Patron item)
        {
            if (item == null || item.patronid != idPatron)
            {
                return BadRequest();
            }
            var patron = patronRepository.FindByID(idPatron);
            if (patron == null)
            {
                return NotFound("Could not find patron with id : " + idPatron + ". Please try again with valid id.");
            }
            patron.fname = item.fname;
            patron.lname = item.lname;
            patron.phone = item.phone;
            patron.emailid = item.emailid;
            var updatedPatron = patronRepository.Update(patron);
            if (updatedPatron == null)
            {
                return StatusCode(500, "Could not update patron with id : " + idPatron + " successfully. Please try again.");
            }
            return Ok("All personal details for patron id:" + idPatron + " are updated successfully! \nFor checking out books, please use checkout interface");
        }



        [HttpDelete("{idPatron}")]
        public IActionResult Delete(long idPatron)
        {
            var patronToDelete = patronRepository.FindByID(idPatron);
            if (patronToDelete == null)
            {
                return NotFound("Could not find patron with id : " + idPatron + ". Please try again with valid id.");
            }
            Patron patron = patronRepository.Remove(idPatron);
            ICollection<Book> temp = patron.checkedoutBooks as ICollection<Book>;
            if (patron.patronid == -1)
            {
                return StatusCode(500, "This patron can not be deleted from database as it has checked out " + temp.Count + " and have not returned");
            }
            return Ok("Patron id :" + idPatron + " deleted successfully!");
        }

        [HttpPut("checkout/{idPatron}", Name = "GetPatronCheckout")]
        public IActionResult Checkout(long idPatron, [FromBody] Patron item)
        {
            if (item == null || item.patronid != idPatron)
            {
                return BadRequest();
            }
            var patron = patronRepository.FindByID(idPatron);
            if (patron == null)
            {
                return NotFound("Could not find patron with id : " + idPatron + ". Please try again with valid id.");
            }
            patron.checkedoutBooks = item.checkedoutBooks;
            Patron updatedPatron = patronRepository.UpdateCheckOut(patron);
            if (updatedPatron.patronid == -1)
            {
                return StatusCode(500, "Could not checkout all mentioned books for patron with id : " + idPatron + ". \nAlready checkedout book id: " + updatedPatron.fname + ". Please try again.");
            }

            //if (updatedPatron.patronid == -2)
            //{
            //    return StatusCode(500, "Could not checkout all mentioned books for patron with id : " + idPatron + ". \nContains invalid book ids. Please try again.");
            //}


                return CreatedAtRoute("GetPatronCheckout", new { idPatron = idPatron }, updatedPatron);
        }


        [HttpPut("return/{idPatron}", Name = "GetPatronReturn")]
        public IActionResult ReturnBooks(long idPatron, [FromBody] Patron item)
        {
            if (item == null || item.patronid != idPatron)
            {
                return BadRequest();
            }
            var patron = patronRepository.FindByID(idPatron);
            if (patron == null)
            {
                return NotFound("Could not find patron with id : " + idPatron + ". Please try again with valid id.");
            }

            patron.checkedoutBooks = item.checkedoutBooks;

            Patron updatedPatron = patronRepository.UpdateReturn(patron);
            if (updatedPatron.patronid == -1)
            {
                return NotFound("Partial books are returned successfully. Book with id : " + updatedPatron.fname + " is already marked as returned");
            }

            return CreatedAtRoute("GetPatronReturn", new { idPatron = idPatron }, updatedPatron);

        }

    }
}
