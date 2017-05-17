using Library.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using System.Data;
using Npgsql;
using System;

namespace Library.Repository
{
    public class BookRepository : IRepository<Book>
    {
        private string connectionString;

        public BookRepository(IConfiguration configuration)
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

        public Book Add(Book book)
        {
            Book lastBookCreated;
            using (IDbConnection dbConnection = conn)
            {
                dbConnection.Open();
                List<MyLibrary> existingLibrary = conn.Query<MyLibrary>("select * from librarydatabases where libraryid=" + book.myLibrary.libraryid).ToList();
                if (book.myLibrary.libraryid != 0)
                {
                    if (existingLibrary.Count == 0)
                    {
                        return null;
                    }
                }
                dbConnection.Execute("INSERT INTO books (title,publisheddate,checkedoutstatus) VALUES (@title,@publisheddate,@checkedoutstatus)", new { title = book.title, publisheddate = book.publisheddate,checkedoutstatus = "false"});

                lastBookCreated = findLastCreatedItem();
                long lastBookID = lastBookCreated.bookid;

                if (book.myLibrary.libraryid == 0)
                {
                    dbConnection.Execute("UPDATE books SET libraryid = default WHERE bookid=" + lastBookID);
                }
                else
                {
                    dbConnection.Execute("UPDATE books SET libraryid ="+ book.myLibrary.libraryid+" WHERE bookid="+lastBookID);
                }


                List<BookAuthor> modifiedBookAuthors = new List<BookAuthor>();
                //Add mapings in bookauthor table
                foreach (var author in book.bookauthor)
                {
                   
                    List<Author> existingAuthor = conn.Query<Author>("select * from authors where authorid=" + author.authorid).ToList();
                    if (existingAuthor.Count == 0)
                    {
                       
                        dbConnection.Execute("DELETE FROM books WHERE bookid=@bookid", new { bookid = lastBookID });
                        return null;
                    }
                    dbConnection.Execute("INSERT INTO bookauthors(bookid,authorid) VALUES (@bookid,@authorid)", new { bookid = lastBookID, authorid = author.authorid });
                }
                lastBookCreated = addBookAuthorsANDLibraryDetails(lastBookID, lastBookCreated);
                lastBookCreated.myLibrary = existingLibrary.FirstOrDefault();
            }
            return lastBookCreated;
        }

        public IEnumerable<Book> FindBooksForAuthor(string searchKeyword)
        {
            searchKeyword = searchKeyword.ToLower();
            List<Book> temp = null;
            using (IDbConnection dbConnection = conn)
            {

                HashSet<Author> authorList = new HashSet<Author>(dbConnection.Query<Author>("SELECT * FROM authors WHERE Lower(fname) LIKE '%" + searchKeyword + "%' OR Lower(lname) LIKE '%" + searchKeyword + "%'").ToList());
                Console.WriteLine("authorList.count= " + authorList.Count());

                temp = new List<Book>();
                foreach (Author author in authorList)
                {
                    List<long> bookIDs = dbConnection.Query<long>("SELECT bookid FROM bookauthors WHERE authorid=" + author.authorid).ToList();
                    Console.WriteLine("bookIDs count = " + bookIDs.Count());
                    foreach (long bookid in bookIDs)
                    {
                        temp.Add(FindByID(bookid));
                    }
                }
            }
            return temp;
        }

        private Book addBookAuthorsANDLibraryDetails(long lastBookID, Book book)
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

        public Book findLastCreatedItem()
        {
            return conn.Query<Book>("select * from books").Last();

        }

        public IEnumerable<Book> FindAll()
        {
            IEnumerable<Book> books = conn.Query<Book>("select * from books");
            List<Book> tempBookList = new List<Book>();

            foreach (Book book in books)
            {
                tempBookList.Add(modify(book));
            }

            return tempBookList;
        }

        public Book modify(Book book)
        {
            IEnumerable<BookAuthor> bookAuthorsMapping = conn.Query<BookAuthor>("select * from bookauthors where bookauthors.bookid=" + book.bookid);
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
                //temp.bookid = book.bookid;
                modifiedBookAuthors.Add(temp);
            }
            IEnumerable<BookAuthor> bookAuthorObjects = modifiedBookAuthors;
            book.bookauthor = bookAuthorObjects;


            List<Book> targetLibraryIdList = conn.Query<Book>("select libraryid from books where books.bookid = " + book.bookid + " and libraryid IS NOT NULL").ToList();

            MyLibrary existingLibrary = null;
            if (targetLibraryIdList.Count() != 0)
            {
                long targetLibraryId = conn.Query<long>("select libraryid from books where books.bookid=" + book.bookid).FirstOrDefault();
                existingLibrary = conn.Query<MyLibrary>("select * from librarydatabases where libraryid=" + targetLibraryId).FirstOrDefault();

            }
            book.myLibrary = existingLibrary;
            
            Book myBook = conn.Query<Book>("select * from books where books.bookid = " + book.bookid).FirstOrDefault();

            Console.WriteLine(" myBook.checkedoutstatus = " + myBook.checkedoutstatus);

            book.patron = null;
            if (myBook.checkedoutstatus.Equals("true"))
            {
                long targetPatronId = conn.Query<long>("select patronid from books where books.bookid = " + book.bookid).FirstOrDefault();

                Patron existingPatron = conn.Query<Patron>("select * from patrons where patronid=" + targetPatronId).FirstOrDefault();

                book.patron = existingPatron;
            }

            return book;
        }

        public Book AddAuthorToBook(Book book)
        {
            IDbConnection dbConnection = conn;
            dbConnection.Open();
            List<BookAuthor> updatedBookAuthorList = book.bookauthor.ToList();
            
            //Check if all entered author ids are valid or not
            foreach (BookAuthor updatedBookAuthor in updatedBookAuthorList)
            {
                Console.WriteLine("\n\n Trying to find out authorid: " + updatedBookAuthor.authorid);
                List<Author> existingAuthor = conn.Query<Author>("select * from authors where authorid=" + updatedBookAuthor.authorid).ToList();
                //if author doesn't exists
                if (existingAuthor.Count == 0)
                {
                    Book temp = new Book();
                    temp.bookid = -1;
                    temp.title = updatedBookAuthor.authorid+"";
                    return temp;
                }
            }

            //Add each author to book. Update bookauthor table mapping
            foreach (BookAuthor updatedBookAuthor in updatedBookAuthorList)
            {
                List<BookAuthor> existingBookAuthor = conn.Query<BookAuthor>("select * from bookauthors where bookid=" + book.bookid + "and authorid=" + updatedBookAuthor.authorid).ToList();

                //if bookauthor mapping doesn't exists, then add new entry. Else don't.
                if (existingBookAuthor.Count == 0)
                {
                    dbConnection.Execute("INSERT INTO bookauthors(bookid,authorid) VALUES (@bookid,@authorid)", new { bookid = book.bookid, authorid = updatedBookAuthor.authorid });
                }
            }

            dbConnection.Close();
            return FindByID(book.bookid);
            
        }

        public Book RemoveAuthorFromBook(Book book)
        {
            IDbConnection dbConnection = conn;
            dbConnection.Open();
            List<BookAuthor> updatedBookAuthorList = book.bookauthor.ToList();
            Console.WriteLine("\n\n RemoveAuthorFromBook");
            //Check if all entered author ids are valid or not
            foreach (BookAuthor updatedBookAuthor in updatedBookAuthorList)
            {
                Console.WriteLine("\n\n @RemoveAuthorFromBook: Trying to find out authorid: " + updatedBookAuthor.authorid);
                List<Author> existingAuthor = conn.Query<Author>("select * from authors where authorid=" + updatedBookAuthor.authorid).ToList();


                //if author doesn't exists
                if (existingAuthor.Count == 0)
                {
                    Book temp = new Book();
                    temp.bookid = -1;
                    temp.title = updatedBookAuthor.authorid + "";
                    return temp;
                }
            }

            //Remove each author to book. Update bookauthor table mapping
            foreach (BookAuthor updatedBookAuthor in updatedBookAuthorList)
            {
                List<BookAuthor> existingBookAuthor = conn.Query<BookAuthor>("select * from bookauthors where bookid=" + book.bookid + "and authorid=" + updatedBookAuthor.authorid).ToList();
                //if bookauthor mapping doesn't exists, then add new entry. Else don't.
                if (existingBookAuthor.Count != 0)
                {
                    //dbConnection.Execute("DELETE FROM bookauthors(bookid,authorid) VALUES (@bookid,@authorid)", new { bookid = book.bookid, authorid = updatedBookAuthor.authorid });
                    dbConnection.Execute("DELETE FROM bookauthors WHERE bookid=@bookid and authorid=@authorid", new { bookid = book.bookid,authorid=updatedBookAuthor.authorid });
                }
            }

            dbConnection.Close();
            return FindByID(book.bookid);

        }

        public Book FindByID(long key)
        {
            IDbConnection dbConnection = conn;
            dbConnection.Open();
            Book book = dbConnection.Query<Book>("SELECT * FROM books WHERE bookid = @bookid", new { bookid = key }).FirstOrDefault();
            if (book == null)
            {
                return null;
            }
            Book temp = modify(book);
            dbConnection.Close();
            return temp;
        }

        public void Remove(long key)
        {
            using (IDbConnection dbConnection = conn)
            {
                dbConnection.Open();
                dbConnection.Execute("DELETE FROM books WHERE bookid=@bookid", new { bookid = key });
                dbConnection.Close();
            }
        }


        public Book Update(Book book)
        {
            using (IDbConnection dbConnection = conn)
            {
                dbConnection.Open();
                //current value in database
                Book currentValue = dbConnection.Query<Book>("SELECT * FROM books WHERE bookid=@bookid", new { bookid = book.bookid }).FirstOrDefault();

                List<MyLibrary> existingLibrary = conn.Query<MyLibrary>("select * from librarydatabases where libraryid=" + book.myLibrary.libraryid).ToList();
                //if library is not existent, do not update and return
                if (existingLibrary.Count == 0)
                {
                    Book status = new Book();
                    //if library code is invalid
                    status.bookid = -1;
                    return status;
                }

               
                dbConnection.Query("UPDATE books SET title = @title,   publisheddate = @publisheddate, libraryid=" + book.myLibrary.libraryid + " WHERE books.bookid = @bookid", book);

                //List<BookAuthor> updatedBookAuthorList = book.bookauthor.ToList();
                //List<BookAuthor> currentBookAuthorList = conn.Query<BookAuthor>("select * from bookauthors where bookid=" + book.bookid).ToList();

                //foreach (BookAuthor currentBookAuthor in currentBookAuthorList)
                //{
                //    dbConnection.Execute("DELETE FROM bookauthors WHERE bookid=@bookid", new { bookid = book.bookid });
                //}


                //List<long> authorIds = new List<long>();

                //foreach (BookAuthor updatedBookAuthor in updatedBookAuthorList)
                //{
                //    List<Author> existingAuthor = conn.Query<Author>("select * from authors where authorid=" + updatedBookAuthor.authorid).ToList();
                //    if (existingAuthor.Count == 0)
                //    {
                //        dbConnection.Query("UPDATE books SET title = @title,   publisheddate = @publisheddate, libraryid=" + currentValue.myLibrary.libraryid + ", WHERE books.bookid = @bookid", currentValue);
                //        return null;
                //    }

                //    List<BookAuthor> existingBookAuthor = conn.Query<BookAuthor>("select * from bookauthors where bookid=" + book.bookid + "and authorid=" + updatedBookAuthor.authorid).ToList();

                //    //if bookauthor mapping doesn't exists, then add new entry. Else don't.
                //    if (existingBookAuthor.Count == 0)
                //    {
                //        dbConnection.Execute("INSERT INTO bookauthors(bookid,authorid) VALUES (@bookid,@authorid)", new { bookid = book.bookid, authorid = updatedBookAuthor.authorid });
                //    }
                //}
                dbConnection.Close();
                return FindByID(book.bookid);
            }
        }

        public Book ChangeLibrary(Book book)
        {

            using (IDbConnection dbConnection = conn)
            {
                dbConnection.Open();

                MyLibrary library=dbConnection.Query<MyLibrary>("SELECT * From librarydatabases where libraryid=" + book.myLibrary.libraryid).FirstOrDefault();

                Book temp = new Book();

                if (library == null)
                {
                    temp.bookid = -1;
                    temp.myLibrary.libraryid = book.myLibrary.libraryid;
                    return temp;
                }

                if(library.libraryid == book.myLibrary.libraryid)
                {
                    temp.bookid = -2;
                    return temp;
                }

                dbConnection.Query("UPDATE books SET libraryid=" + book.myLibrary.libraryid + " WHERE books.bookid = @bookid", book);
                dbConnection.Close();
            }

            return FindByID(book.bookid);
        }
    }
}
