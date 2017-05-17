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
    public class AuthorController : Controller
    {
        private readonly AuthorRepository authorRepository;

        public AuthorController(IConfiguration configuration)
        {
            authorRepository = new AuthorRepository(configuration);
        }

        [HttpGet]
        public IEnumerable<Author> GetAll()
        {
            return authorRepository.FindAll();
        }

        [HttpGet("{id}", Name = "GetAuthor")]
        public IActionResult GetById(long id)
        {
            var item = authorRepository.FindByID(id);
            if (item == null)
            {
                return NotFound("Author with id : " + id + " is not present. Enter valid author id.");
            }
            return Ok(item);
        }


        [HttpPost]
        public IActionResult Create([FromBody] Author item)
        {
            if (item == null)
            {
                return BadRequest();
            }
            Author lastItem = authorRepository.Add(item);
            if (lastItem == null)
            {
                return StatusCode(500, "Could not add last item successfully. Please try again.");
            }
            long newID = lastItem.authorid;
            return CreatedAtRoute("GetAuthor", new { id = newID }, lastItem);
        }

        [HttpPut("{id}")]
        public IActionResult Update(long id, [FromBody] Author item)
        {
            if (item == null || item.authorid != id)
            {
                return BadRequest();
            }

            var foundAuthor = authorRepository.FindByID(id);
            if (foundAuthor == null)
            {
                return NotFound("Could not find author with id : " + item.authorid + ". Please try again with valid id.");
            }

            foundAuthor.fname = item.fname;
            foundAuthor.lname = item.lname;
            foundAuthor.bookauthor = item.bookauthor;

            var updatedAuthor = authorRepository.Update(foundAuthor);

            if (updatedAuthor == null)
            {
                return StatusCode(500, "Could not update author with id : " + item.authorid + " successfully. Please try again.");
            }

            return Ok("Author id : " + updatedAuthor.authorid + " updated successfully");
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(long id)
        {
            var authorDeleted = authorRepository.FindByID(id);
            if (authorDeleted == null)
            {
                return NotFound("Could not find author with id : " + id + ". Please try again with valid id.");
            }

            authorRepository.Remove(id);
            return Ok("Author id :" + authorDeleted.authorid + " deleted successfully!");
        }


        [HttpPut("addBookToAuthor/{idAuthor}", Name = "AddBook")]
        public IActionResult AddAuthorToBook(long idAuthor, [FromBody] Author item)
        {
            if (item == null || item.authorid != idAuthor)
            {
                return BadRequest();
            }
            var author = authorRepository.FindByID(idAuthor);
            if (author == null)
            {
                return NotFound("Could not find book with id : " + idAuthor + ". Please try again with valid id.");
            }

            author.bookauthor = item.bookauthor;

            Author updatedBook = authorRepository.AddAuthorToBook(author);

            Console.WriteLine(" updatedBook = " + (updatedBook == null));

            if (updatedBook.authorid == -1)
            {
                return NotFound("Invalid author id : " + updatedBook.fname + ". Please try again");
            }

            return CreatedAtRoute("AddBook", new { idBook = idAuthor }, updatedBook);
        }



        [HttpPut("removeAuthorFromBook/{idBook}", Name = "RemoveBook")]
        public IActionResult RemoveAuthorFromBook(long idBook, [FromBody] Author item)
        {
            if (item == null || item.authorid != idBook)
            {
                return BadRequest();
            }
            var author = authorRepository.FindByID(idBook);
            if (author == null)
            {
                return NotFound("Could not find book with id : " + idBook + ". Please try again with valid id.");
            }

            author.bookauthor = item.bookauthor;
           
            Author updatedAuthor = authorRepository.RemoveBookFromAuthor(author);

            if (updatedAuthor.authorid == -1)
            {
                return NotFound("Invalid author id : " + updatedAuthor.fname + ". Please try again");
            }

            return CreatedAtRoute("RemoveBook", new { idBook = idBook }, updatedAuthor);
        }

    }
}
