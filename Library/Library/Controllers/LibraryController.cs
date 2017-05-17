using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Library.Repository;
using Microsoft.Extensions.Configuration;
using Library.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Library.Controllers
{
    [Route("api/[controller]")]
    public class LibraryController : Controller
    {
        private readonly LibraryRepository libraryRepository;

        public LibraryController(IConfiguration configuration)
        {
            libraryRepository = new LibraryRepository(configuration);
        }

        [HttpGet]
        public IEnumerable<MyLibrary> GetAll()
        {
            return libraryRepository.FindAll();
        }

        [HttpGet("{idLibrary}", Name = "GetLibrary")]
        public IActionResult GetById(long idLibrary)
        {
            var item = libraryRepository.FindByID(idLibrary);
            if (item == null)
            {
                return NotFound("Library with id : " + item.libraryid + " is not present. Enter valid library id.");
            }
            return Ok(item);
        }

        [HttpPost]
        public IActionResult Create([FromBody] MyLibrary item)
        {
            if (item == null)
            {
                return BadRequest();
            }
            MyLibrary lastItem = libraryRepository.Add(item);
            if (lastItem == null)
            {
                return StatusCode(500, "Could not add last item successfully. Please try again.");
            }
            long newID = lastItem.libraryid;
            return CreatedAtRoute("GetLibrary", new { idLibrary = newID }, lastItem);
        }

        [HttpPut("{idLibrary}")]
        public IActionResult Update(long idLibrary, [FromBody] MyLibrary item)
        {
            if (item == null || item.libraryid != idLibrary)
            {
                return BadRequest();
            }

            var libraryToUpdate = libraryRepository.FindByID(idLibrary);
            if (libraryToUpdate == null)
            {
                return NotFound("Could not find library with id : " + idLibrary + ". Please try again with valid id.");
            }

            libraryToUpdate.libraryname = item.libraryname;
            libraryToUpdate.libraryaddress = item.libraryaddress;
            libraryToUpdate.libraryphone = item.libraryphone;


            var libraryUpdated = libraryRepository.Update(libraryToUpdate);

            if (libraryUpdated == null)
            {
                return StatusCode(500, "Could not update author with id : " + idLibrary + " successfully. Please try again.");
            }

            return Ok("Library id : " + libraryUpdated.libraryid + " updated successfully");
        }

        [HttpDelete("{idLibrary}")]
        public IActionResult Delete(long idLibrary)
        {
            var todo = libraryRepository.FindByID(idLibrary);
            if (todo == null)
            {
                return NotFound("Could not find library with id : " + idLibrary + ". Please try again with valid id.");
            }

            libraryRepository.Remove(idLibrary);
            return Ok("Library id :" + idLibrary + " deleted successfully!");
        }


        [HttpPut("addBookToLibrary/{idLibrary}", Name = "AddBookToLibrary")]
        public IActionResult AddBookToLibrary(long idLibrary, [FromBody] MyLibrary item)
        {
            if (item == null || item.libraryid != idLibrary)
            {
                return BadRequest();
            }
            var library = libraryRepository.FindByID(idLibrary);
            if (library == null)
            {
                return NotFound("Could not find library with id : " + idLibrary + ". Please try again with valid id.");
            }

            library.myBooks = item.myBooks;

            MyLibrary updatedLibrary = libraryRepository.AddBookToLibrary(library);

            if (updatedLibrary.libraryid == -1)
            {
                return NotFound("Invalid book id : " + updatedLibrary.libraryname + ". Please try again");
            }

            if (updatedLibrary.libraryid == -2)
            {
                return NotFound("Book id : " + updatedLibrary.libraryname + " already present in same library. Please try again by removing that id");
            }

            if (updatedLibrary.libraryid == -3)
            {
                return NotFound("Book id : " + updatedLibrary.libraryname + " is present in another library. \nPlease use ChangeLibrary endpoint if you want to move book to another library");
            }

            return CreatedAtRoute("AddBookToLibrary", new { idLibrary = idLibrary }, updatedLibrary);
        }



        [HttpPut("removeBookFromLibrary/{idLibrary}", Name = "RemoveBookFromLibrary")]
        public IActionResult RemoveAuthorFromBook(long idLibrary, [FromBody] MyLibrary item)
        {
            if (item == null || item.libraryid != idLibrary)
            {
                return BadRequest();
            }
            var library = libraryRepository.FindByID(idLibrary);
            if (library == null)
            {
                return NotFound("Could not find book with id : " + idLibrary + ". Please try again with valid id.");
            }

            library.myBooks = item.myBooks;
          
            MyLibrary updatedLibrary = libraryRepository.RemoveBookFromLibrary(library);

            if (updatedLibrary.libraryid == -1)
            {
                return NotFound("Invalid author id : " + updatedLibrary.libraryname + ". Please try again");
            }

            if (updatedLibrary.libraryid == -2)
            {
                return NotFound("Book id : " + updatedLibrary.libraryname + " is not present in mentioned library. Please try again by removing that id");
            }

            if (updatedLibrary.libraryid == -3)
            {
                return NotFound("Book id : " + updatedLibrary.libraryname + " is present in another library. \nPlease try again with correct id");
            }

            return CreatedAtRoute("RemoveBookFromLibrary", new { idLibrary = idLibrary }, updatedLibrary);
        }
    }

}
