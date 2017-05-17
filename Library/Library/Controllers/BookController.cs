using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Library.Repository;
using Library.Models;
using Microsoft.Extensions.Configuration;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Library.Controllers
{
    [Route("api/[controller]")]
    public class BookController : Controller
    {
        private readonly BookRepository bookRepository;

        public BookController(IConfiguration configuration)
        {
            bookRepository = new BookRepository(configuration);
        }

        [HttpGet]
        public IEnumerable<Book> GetAll()
        {
            return bookRepository.FindAll();
        }

        [HttpGet("{id}", Name = "GetTodo")]
        public IActionResult GetById(long id)
        {
            var item = bookRepository.FindByID(id);
            if (item == null)
            {
                return NotFound("Book with id : " + id + " is not present. Enter valid book id.");
            }
            return Ok(item);
        }

        [HttpGet("searchByAuthor/{searchkeyword}", Name = "GetSearchByAuthor")]
        public IActionResult GetBooksByAuthor(string searchKeyword)
        {
            var item = bookRepository.FindBooksForAuthor(searchKeyword);
            return Ok(item);
        }

        [HttpPost]
        public IActionResult Create([FromBody] Book item)
        {
            if (item == null)
            {
                return BadRequest();
            }

            if (item.bookauthor == null)
            {
                Console.WriteLine("BookAuthor is NULL");
            }

            Book b = new Book();
            b.bookauthor = item.bookauthor;
            b.bookid = item.bookid;
            b.checkedoutstatus = item.checkedoutstatus;
            b.myLibrary = item.myLibrary;
            b.patron = item.patron;
            b.publisheddate = item.publisheddate;
            b.title = item.title;

            foreach (BookAuthor br in b.bookauthor)
            {
                Console.WriteLine("COUNT = " + item.bookauthor.Count());
                Console.WriteLine("br = " + br.authorid);
            }

            Console.WriteLine("Inside controller BOOK : " + item.bookauthor);

            Book lastItem = bookRepository.Add(item);

            if (lastItem == null)
            {
                return StatusCode(500, "Could not add last item successfully. Please try again.");
            }

            long newID = lastItem.bookid;

            return CreatedAtRoute("GetTodo", new { id = newID }, lastItem);
        }

        [HttpPut("{id}")]
        public IActionResult Update(long id, [FromBody] Book item)
        {
            if (item == null || item.bookid != id)
            {
                return BadRequest();
            }

            var book = bookRepository.FindByID(id);
            if (book == null)
            {
                return NotFound("Could not find book with id : " + item.bookid + ". Please try again with valid id.");
            }

            book.publisheddate = item.publisheddate;
            book.title = item.title;
            book.myLibrary = item.myLibrary;
            book.bookauthor = item.bookauthor;
            book.checkedoutstatus = item.checkedoutstatus;
            book.patron = item.patron;

            var UpdatedBook = bookRepository.Update(book);
            if (UpdatedBook == null)
            {
                return StatusCode(500, "Could not update book with id : " + item.bookid + " successfully. Please try again.");
            }

            if (UpdatedBook.bookid == -1)
            {
                return StatusCode(500, "Invalid ids in request. Please enter valid id.");
            }

            return Ok("Book id : " + UpdatedBook.bookid + " updated successfully");
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(long id)
        {
            var bookDeleted = bookRepository.FindByID(id);
            if (bookDeleted == null)
            {
                return NotFound("Could not find book with id : " + id + ". Please try again with valid id.");
            }

            bookRepository.Remove(id);
            return Ok("Book id :" + bookDeleted.bookid + " deleted successfully!");
        }


        [HttpPut("changeLibrary/{idBook}", Name = "ChangeLibrary")]
        public IActionResult ChangeLibraryForBook(long idBook, [FromBody] Book item)
        {
            if (item == null || item.bookid != idBook)
            {
                return BadRequest();
            }
            var book = bookRepository.FindByID(idBook);
            if (book == null)
            {
                return NotFound("Could not find book with id : " + idBook + ". Please try again with valid id.");
            }

            book.myLibrary = item.myLibrary;

            Book updatedBook = bookRepository.ChangeLibrary(book);
            if (updatedBook.bookid == -1)
            {
                return NotFound("Destination library id : "+updatedBook.myLibrary.libraryid+" is not valid. Please try again");
            }

            if(updatedBook.bookid == -2)
            {
                return NotFound("Destination library id : " + updatedBook.myLibrary.libraryid + " is same as source library id.");
            }

            return CreatedAtRoute("ChangeLibrary", new { idBook = idBook }, updatedBook);
        }


        [HttpPut("addAuthorToBook/{idBook}", Name = "AddAuthor")]
        public IActionResult AddAuthorToBook(long idBook, [FromBody] Book item)
        {
            if (item == null || item.bookid != idBook)
            {
                return BadRequest();
            }
            var book = bookRepository.FindByID(idBook);
            if (book == null)
            {
                return NotFound("Could not find book with id : " + idBook + ". Please try again with valid id.");
            }

            Console.WriteLine("item.bookid = " + item.bookid);

            foreach(BookAuthor b in item.bookauthor)
            {
                Console.WriteLine("\n At COntroller, authorid = " + b.authorid+"\n");
            }
            
            book.bookauthor = item.bookauthor;

            Book updatedBook = bookRepository.AddAuthorToBook(book);

            Console.WriteLine(" updatedBook = " + (updatedBook == null));

            if (updatedBook.bookid == -1)
            {
                return NotFound("Invalid author id : " + updatedBook.title + ". Please try again");
            }

            return CreatedAtRoute("AddAuthor", new { idBook = idBook }, updatedBook);
        }



        [HttpPut("removeAuthorFromBook/{idBook}", Name = "RemoveAuthor")]
        public IActionResult RemoveAuthorFromBook(long idBook, [FromBody] Book item)
        {
            if (item == null || item.bookid != idBook)
            {
                return BadRequest();
            }
            var book = bookRepository.FindByID(idBook);
            if (book == null)
            {
                return NotFound("Could not find book with id : " + idBook + ". Please try again with valid id.");
            }

            book.bookauthor = item.bookauthor;
            Console.WriteLine("\n@Controller: RemoveAuthorFromBook");
            Book updatedBook = bookRepository.RemoveAuthorFromBook(book);
            
            if (updatedBook.bookid == -1)
            {
                return NotFound("Invalid author id : " + updatedBook.title + ". Please try again");
            }

            return CreatedAtRoute("RemoveAuthor", new { idBook = idBook }, updatedBook);
        }
    }
}
