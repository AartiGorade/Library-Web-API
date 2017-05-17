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
    public class AuthorRepository : IRepository<Author>
    {
        private string connectionString;
        public AuthorRepository(IConfiguration configuration)
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

        public Author Modify(Author author)
        {
            IEnumerable<BookAuthor> bookAuthorsMapping = conn.Query<BookAuthor>("select * from bookauthors where bookauthors.authorid=" + author.authorid);
            List<long> bookIDList = new List<long>();
            foreach (BookAuthor bookAuthor in bookAuthorsMapping)
            {
                bookIDList.Add(bookAuthor.bookid);
            }
            IEnumerable<Book> bookObjects = null;
            List<Book> tempList = new List<Book>();
            foreach (long id in bookIDList)
            {
                List<Book> bObjects = conn.Query<Book>("select * from books where bookid=" + id).ToList();
                tempList.AddRange(bObjects);
            }
            bookObjects = tempList;
            List<BookAuthor> modifiedBookAuthors = new List<BookAuthor>();
            foreach (Book eachBook in bookObjects)
            {
                BookAuthor temp = new BookAuthor();
                
                List<Book> targetLibraryIdList = conn.Query<Book>("select libraryid from books where books.bookid = " + eachBook.bookid + " and libraryid IS NOT NULL").ToList();

                MyLibrary existingLibrary = null;
                if (targetLibraryIdList.Count() != 0)
                {
                    long targetLibraryId = conn.Query<long>("select libraryid from books where books.bookid=" + eachBook.bookid).FirstOrDefault();
                    existingLibrary = conn.Query<MyLibrary>("select * from librarydatabases where libraryid=" + targetLibraryId).FirstOrDefault();

                }
                eachBook.myLibrary = existingLibrary;

                if (eachBook.checkedoutstatus.Equals("true"))
                {
                    long patronID = conn.Query<long>("select patronid from books where books.bookid=" + eachBook.bookid).FirstOrDefault();
                    Patron myPatron = conn.Query<Patron>("select * from patrons where patronid=" + patronID).FirstOrDefault();

                    eachBook.patron = myPatron;
                }

                temp.book = eachBook;
                //temp.bookid = eachBook.bookid;
                //temp.authorid = author.authorid;
                modifiedBookAuthors.Add(temp);
            }
            IEnumerable<BookAuthor> bookAuthorObjects = modifiedBookAuthors;
            author.bookauthor = bookAuthorObjects;
            return author;
        }

        public Author Add(Author author)
        {
            IEnumerable<Author> createdItems;
            using (IDbConnection dbConnection = conn)
            {
                dbConnection.Open();
                dbConnection.Execute("INSERT INTO authors (fname,lname) VALUES (@fname,@lname)", author);
                createdItems = FindAll();
                long lastAuthorID = createdItems.Last().authorid;
                foreach (var book in author.bookauthor)
                {
                    List<Book> existingBook = conn.Query<Book>("select * from books where bookid=" + book.bookid).ToList();
                    if (existingBook.Count == 0)
                    {
                        dbConnection.Execute("DELETE FROM authors WHERE authorid=@authorid", new { authorid = lastAuthorID });
                        return null;
                    }
                    dbConnection.Execute("INSERT INTO bookauthors(bookid,authorid) VALUES (@bookid,@authorid)", new { bookid = book.bookid, authorid = lastAuthorID });
                }
                dbConnection.Close();
            }
            return createdItems.Last();
        }

        public Author AddAuthorToBook(Author author)
        {
            IDbConnection dbConnection = conn;
            dbConnection.Open();
            List<BookAuthor> updatedBookAuthorList = author.bookauthor.ToList();
            List<long> authorIds = new List<long>();
            //Check if all entered book ids are valid or not
            foreach (BookAuthor updatedBookAuthor in updatedBookAuthorList)
            {
                List<Author> existingAuthor = conn.Query<Author>("select * from books where bookid=" + updatedBookAuthor.bookid).ToList();
                //if author doesn't exists
                if (existingAuthor.Count == 0)
                {
                    Author temp = new Author();
                    temp.authorid = -1;
                    temp.fname = updatedBookAuthor.bookid + "";
                    return temp;
                }
            }

            //Add each author to book. Update bookauthor table mapping
            foreach (BookAuthor updatedBookAuthor in updatedBookAuthorList)
            {
                List<BookAuthor> existingBookAuthor = conn.Query<BookAuthor>("select * from bookauthors where authorid=" + author.authorid + "and bookid=" + updatedBookAuthor.bookid).ToList();

                //if bookauthor mapping doesn't exists, then add new entry. Else don't.
                if (existingBookAuthor.Count == 0)
                {
                    dbConnection.Execute("INSERT INTO bookauthors(bookid,authorid) VALUES (@bookid,@authorid)", new { authorid = author.authorid, bookid = updatedBookAuthor.bookid });
                }
            }

            dbConnection.Close();
            return FindByID(author.authorid);

        }

        public IEnumerable<Author> FindAll()
        {
            IEnumerable<Author> authors = conn.Query<Author>("select * from authors");
            List<Author> tempAuthorList = new List<Author>();
            foreach (Author author in authors)
            {
                tempAuthorList.Add(Modify(author));
            }
            return tempAuthorList;
        }


        public Author FindByID(long key)
        {
            IDbConnection dbConnection = conn;
            dbConnection.Open();
            Author author = dbConnection.Query<Author>("SELECT * FROM authors WHERE authorid = @authorid", new { authorid = key }).FirstOrDefault();
            dbConnection.Close();
            if (author == null)
            {
                return null;
            }
            author = Modify(author);
            return author;
        }

        public void Remove(long key)
        {
            using (IDbConnection dbConnection = conn)
            {
                dbConnection.Open();
                dbConnection.Execute("DELETE FROM authors WHERE authorid=@authorid", new { authorid = key });
                dbConnection.Execute("DELETE FROM bookauthors WHERE bookid=@bookid", new { bookid = key });
                dbConnection.Close();
            }
        }

        internal Author RemoveBookFromAuthor(Author author)
        {
            IDbConnection dbConnection = conn;
            dbConnection.Open();
            List<BookAuthor> updatedBookAuthorList = author.bookauthor.ToList();
            List<long> authorIds = new List<long>();
            Console.WriteLine("\n\n RemoveAuthorFromBook");
            //Check if all entered author ids are valid or not
            foreach (BookAuthor updatedBookAuthor in updatedBookAuthorList)
            {
                Console.WriteLine("\n\n @RemoveAuthorFromBook: Trying to find out authorid: " + updatedBookAuthor.authorid);
                List<Author> existingAuthor = conn.Query<Author>("select * from authors where authorid=" + updatedBookAuthor.authorid).ToList();
                //if author doesn't exists
                if (existingAuthor.Count == 0)
                {
                    Author temp = new Author();
                    temp.authorid = -1;
                    temp.fname = updatedBookAuthor.bookid + "";
                    return temp;
                }
            }

            //Remove each author to book. Update bookauthor table mapping
            foreach (BookAuthor updatedBookAuthor in updatedBookAuthorList)
            {
                List<BookAuthor> existingBookAuthor = conn.Query<BookAuthor>("select * from bookauthors where authorid=" + author.authorid + "and bookid=" + updatedBookAuthor.bookid).ToList();

                //if bookauthor mapping doesn't exists, then add new entry. Else don't.
                if (existingBookAuthor.Count != 0)
                {
                    dbConnection.Execute("DELETE FROM bookauthors WHERE bookid=@bookid and authorid=@authorid", new { authorid = author.authorid, bookid = updatedBookAuthor.bookid });
                }
            }

            dbConnection.Close();
            return FindByID(author.authorid);
        }

        public Author Update(Author author)
        {
            using (IDbConnection dbConnection = conn)
            {
                dbConnection.Open();
                Author currentValue = dbConnection.Query<Author>("SELECT * FROM authors WHERE authors.authorid=@authorid", author).FirstOrDefault();

                dbConnection.Query("UPDATE authors SET fname = @fname,   lname = @lname WHERE authorid = @authorid", author);

                //List<BookAuthor> updatedBookAuthorList = author.bookauthor.ToList();
                //List<BookAuthor> currentBookAuthorList = conn.Query<BookAuthor>("select * from bookauthors where authorid=" + author.authorid).ToList(); ;
                //List<long> bookIds = new List<long>();
                //foreach (BookAuthor currentBookAuthor in currentBookAuthorList)
                //{
                //    bookIds.Add(currentBookAuthor.bookid);
                //}
                //foreach (BookAuthor updatedBookAuthor in updatedBookAuthorList)
                //{
                //    if (!bookIds.Contains(updatedBookAuthor.bookid))
                //    {
                //        List<Book> existingBook = conn.Query<Book>("select * from books where bookid=" + updatedBookAuthor.bookid).ToList();
                //        if (existingBook.Count == 0)
                //        {
                //            dbConnection.Query("UPDATE authors SET fname = @fname,   lname = @lname WHERE authorid = @authorid", currentValue);
                //            return null;
                //        }
                //        dbConnection.Execute("INSERT INTO bookauthors(bookid,authorid) VALUES (@bookid,@authorid)", new { bookid = updatedBookAuthor.bookid, authorid = author.authorid });
                //    }
                //}
                dbConnection.Close();
                return author;
            }
        }
    }
}
