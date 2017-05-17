using Dapper;
using Library.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;


namespace Library.Repository
{
    public class LibraryRepository : IRepository<MyLibrary>
    {
        private string connectionString;
        public LibraryRepository(IConfiguration configuration)
        {
            connectionString = configuration.GetValue<string>("DBInfo:ConnectionString");
        }

        internal IDbConnection conn
        {
            get
            {
                return new NpgsqlConnection(connectionString);
            }
        }

        public MyLibrary Add(MyLibrary myLibrary)
        {
            IEnumerable<MyLibrary> createdItems;
            using (IDbConnection dbConnection = conn)
            {
                dbConnection.Open();
                dbConnection.Execute("INSERT INTO librarydatabases (libraryname,libraryaddress,libraryphone) VALUES (@libraryname,@libraryaddress,@libraryphone)", myLibrary);
                createdItems = FindAll();
            }
            return createdItems.Last();
        }

        public IEnumerable<MyLibrary> FindAll()
        {
            IEnumerable<MyLibrary> myLibraries = conn.Query<MyLibrary>("select * from librarydatabases");
            foreach (MyLibrary library in myLibraries)
            {
                IEnumerable<Book> bookLibraryMapping = conn.Query<Book>("select * from books where books.libraryid=" + library.libraryid);
                List<long> booksIDList = new List<long>();
                foreach (Book book in bookLibraryMapping)
                {
                    booksIDList.Add(book.bookid);
                }
                List<Book> tempList = new List<Book>();
                foreach (long id in booksIDList)
                {
                    Book bookObject = conn.Query<Book>("select * from books where bookid=" + id).ToList().FirstOrDefault();
                    bookObject.myLibrary = null;

                    if (bookObject.checkedoutstatus.Equals("true"))
                    {
                        long patronID = conn.Query<long>("select patronid from books where books.bookid=" + id).FirstOrDefault();
                        Patron myPatron = conn.Query<Patron>("select * from patrons where patronid=" + patronID).FirstOrDefault();
                        bookObject.patron = myPatron;
                    }
                    tempList.Add(bookObject);
                }
                if (tempList.Count() != 0)
                {
                    library.myBooks = tempList;
                }
                
            }
            return myLibraries;
        }

        public MyLibrary FindByID(long key)
        {
            IDbConnection dbConnection = conn;
            dbConnection.Open();
            MyLibrary library = dbConnection.Query<MyLibrary>("SELECT * FROM librarydatabases WHERE libraryid = @libraryid", new { libraryid = key }).FirstOrDefault();
            if (library == null)
            {
                return null;
            }
            List<long> booksIDList = conn.Query<long>("select books.bookid from books where books.libraryid=" + library.libraryid).ToList();


            List<Book> tempList = new List<Book>();
            foreach (long id in booksIDList)
            {
                Book bookObject = conn.Query<Book>("select * from books where bookid=" + id).FirstOrDefault();
                bookObject.myLibrary = null;

                if (bookObject.checkedoutstatus.Equals("true"))
                {
                    long targetPatronId = conn.Query<long>("select patronid from books where books.bookid = " + bookObject.bookid).FirstOrDefault();

                    Patron existingPatron = conn.Query<Patron>("select * from patrons where patronid=" + targetPatronId).FirstOrDefault();

                    bookObject.patron = existingPatron;
                }

                tempList.Add(addBookAuthors(bookObject.bookid,bookObject));
            }
            if (tempList.Count() != 0)
            {
                library.myBooks = tempList;
            }

            dbConnection.Close();
            return library;
        }

        public MyLibrary AddBookToLibrary(MyLibrary library)
        {
            IDbConnection dbConnection = conn;
            dbConnection.Open();
            List<Book> updatedBooksList = library.myBooks.ToList();

            //Check if all entered book ids are valid or not
            foreach (Book updatedBook in updatedBooksList)
            {
                List<Book> existingBook = conn.Query<Book>("select * from books where bookid=" + updatedBook.bookid).ToList();
                //if book doesn't exists
                if (existingBook.Count == 0)
                {
                    MyLibrary temp = new MyLibrary();
                    temp.libraryid = -1;
                    temp.libraryname = updatedBook.bookid + "";
                    return temp;
                }

                long existingLibraryId = 0;
                //To check if library id is null
                List<Book> fetchBook = conn.Query<Book>("select * from books where bookid=" + updatedBook.bookid+ " and libraryid IS NOT NULL").ToList();

                //if library id is not null, then fetch actual value
                if (fetchBook.Count() != 0)
                {
                    existingLibraryId = conn.Query<long>("select libraryid from books where bookid=" + updatedBook.bookid).FirstOrDefault();
                }

                //if book already exists in the library
                if (existingLibraryId == library.libraryid)
                {
                    MyLibrary temp = new MyLibrary();
                    temp.libraryid = -2;
                    temp.libraryname = updatedBook.bookid + "";
                    return temp;
                }

                //Book is present in another library
                if (existingLibraryId != 0 && existingLibraryId != library.libraryid)
                {
                    MyLibrary temp = new MyLibrary();
                    temp.libraryid = -3;
                    temp.libraryname = updatedBook.bookid + "";
                    return temp;
                }

            }

            //Add each book to Library. Update book table mapping
            foreach (Book updatedBook in updatedBooksList)
            {                
                dbConnection.Query("UPDATE books SET libraryid=" + library.libraryid + " WHERE books.bookid ="+ updatedBook.bookid);
            }

            dbConnection.Close();
            return FindByID(library.libraryid);

        }

        public void Remove(long key)
        {
            using (IDbConnection dbConnection = conn)
            {
                dbConnection.Open();
                dbConnection.Execute("DELETE FROM librarydatabases WHERE libraryid=@libraryid", new { libraryid = key });
                dbConnection.Close();
            }
        }



        public MyLibrary Update(MyLibrary myLibrary)
        {
            using (IDbConnection dbConnection = conn)
            {
                dbConnection.Open();
                Console.WriteLine("\n\n I am here");

                dbConnection.Query("UPDATE librarydatabases SET libraryname = @libraryname,   libraryaddress = @libraryaddress, libraryphone=@libraryphone WHERE libraryid = @libraryid", myLibrary);

                dbConnection.Close();
            }
            return myLibrary;
        }

        public MyLibrary RemoveBookFromLibrary(MyLibrary library)
        {
            IDbConnection dbConnection = conn;
            dbConnection.Open();
            List<Book> updatedBooksList = library.myBooks.ToList();

            //Check if all entered book ids are valid or not
            foreach (Book updatedBook in updatedBooksList)
            {
                Book existingBook = conn.Query<Book>("select * from books where bookid=" + updatedBook.bookid).FirstOrDefault();
                //if book id is not valid
                if (existingBook == null)
                {
                    MyLibrary temp = new MyLibrary();
                    temp.libraryid = -1;
                    temp.libraryname = updatedBook.bookid + "";
                    return temp;
                }

                long existingLibraryId = 0;
                
                List<Book> fetchBook = conn.Query<Book>("select libraryid from books where bookid=" + updatedBook.bookid+ " and libraryid IS NOT NULL").ToList();

                Console.WriteLine(" fetchBook = " + fetchBook);

                if (fetchBook.Count() != 0)
                {
                    existingLibraryId= conn.Query<long>("select libraryid from books where bookid=" + updatedBook.bookid).FirstOrDefault();
                }
                
                //if book does not exist in the library
                if (existingLibraryId == 0)
                {
                    MyLibrary temp = new MyLibrary();
                    temp.libraryid = -2;
                    temp.libraryname = updatedBook.bookid + "";
                    return temp;
                }

                //Book is present in another library
                if ( existingLibraryId != library.libraryid)
                {
                    MyLibrary temp = new MyLibrary();
                    temp.libraryid = -3;
                    temp.libraryname = updatedBook.bookid + "";
                    return temp;
                }

            }

            //Remove each book to Library. Update book table mapping
            foreach (Book updatedBook in updatedBooksList)
            {
                dbConnection.Query("UPDATE books SET libraryid= default WHERE books.bookid =" + updatedBook.bookid);
            }

            dbConnection.Close();
            return FindByID(library.libraryid);

        }


        private Book addBookAuthors(long lastBookID, Book book)
        {

            IEnumerable<BookAuthor> bookAuthorsMapping = conn.Query<BookAuthor>("select * from bookauthors where bookauthors.bookid=" + lastBookID);
            List<long> authorIDList = new List<long>();
            foreach (BookAuthor bookAuthor in bookAuthorsMapping)
            {
                authorIDList.Add(bookAuthor.authorid);
            }
            IEnumerable<Author> authorObjects = null;
            List<Author> tempList = new List<Author>();
            foreach (long id in authorIDList)
            {
                List<Author> bObjects = conn.Query<Author>("select * from authors where authorid=" + id).ToList();
                tempList.AddRange(bObjects);
            }
            authorObjects = tempList;
            List<BookAuthor> modifiedBookAuthors = new List<BookAuthor>();
            foreach (Author eachAuthor in authorObjects)
            {
                BookAuthor temp = new BookAuthor();
                temp.author = eachAuthor;
                //temp.authorid = eachAuthor.authorid;
                //temp.bookid = lastBookID;
                modifiedBookAuthors.Add(temp);
            }
            IEnumerable<BookAuthor> bookAuthorObjects = modifiedBookAuthors;
            book.bookauthor = bookAuthorObjects;
            book.bookid = lastBookID;
            
            return book;
        }
    }
}
