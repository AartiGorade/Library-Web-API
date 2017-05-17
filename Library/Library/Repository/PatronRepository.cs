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
    public class PatronRepository : IPatron<Patron>
    {
        private string connectionString;
        private double lendingPeriod=7.0;

        public PatronRepository(IConfiguration configuration)
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

        public Patron Add(Patron patron)
        {
            IEnumerable<Patron> createdItems;
            using (IDbConnection dbConnection = conn)
            {
                dbConnection.Open();
                dbConnection.Execute("INSERT INTO patrons (fname,lname,phone,emailid) VALUES (@fname,@lname,@phone,@emailid)", patron);
                createdItems = FindAll();
            }
            return createdItems.Last();
        }

        public IEnumerable<Patron> FindAll()
        {
            IEnumerable<Patron> patrons = conn.Query<Patron>("select * from patrons");
            List<Patron> tempPatronList = new List<Patron>();

            foreach (Patron patron in patrons)
            {
                tempPatronList.Add(ModifyPatronAuthorAndLibrary(patron));
            }
            return tempPatronList;
        }

        private Patron ModifyPatronAuthorAndLibrary(Patron patron)
        {
            Console.WriteLine("\n\n $$$ INSIDE ModifyPatronAuthorAndLibrary()  ");
            List<long> booksIDList = conn.Query<long>("select bookid from books where books.patronid=" + patron.patronid).ToList();
            List<Book> tempList = new List<Book>();
            foreach (long id in booksIDList)
            {
                Book bookObject = conn.Query<Book>("select * from books where bookid=" + id).ToList().FirstOrDefault();
                bookObject.patron = null;
                bookObject = addBookAuthorsANDLibraryDetails(id, bookObject);
                tempList.Add(bookObject);
            }

            if (tempList.Count != 0)
            {
                patron.checkedoutBooks = tempList;
            }
            return patron;
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
            book.bookauthor = modifiedBookAuthors;
            book.bookid = lastBookID;
            Book bookObject = conn.Query<Book>("select * from books where books.bookid=" + lastBookID).FirstOrDefault();
            book.checkedoutstatus = bookObject.checkedoutstatus;

            List<Book> libraryIDList = conn.Query<Book>("select libraryid from books where books.bookid=" + lastBookID+" and libraryid IS NOT NULL").ToList();


            MyLibrary existingLibrary = null;
            if (libraryIDList.Count() != 0)
            {
                long libraryID = conn.Query<long>("select libraryid from books where books.bookid=" + lastBookID).FirstOrDefault();
                existingLibrary = conn.Query<MyLibrary>("select * from librarydatabases where libraryid=" + libraryID).FirstOrDefault();

            }
            book.myLibrary = existingLibrary;
            return book;
        }

        public Patron FindByID(long key)
        {
            IDbConnection dbConnection = conn;
            dbConnection.Open();
            Patron patron = dbConnection.Query<Patron>("SELECT * FROM patrons WHERE patronid = @patronid", new { patronid = key }).FirstOrDefault();
            if (patron == null)
            {
                return null;
            }
            patron = ModifyPatronAuthorAndLibrary(patron);
            dbConnection.Close();
            return patron;
        }

        public Patron Remove(long key)
        {
            Patron patron = new Patron();
            using (IDbConnection dbConnection = conn)
            {
                dbConnection.Open();
                dbConnection.Execute("DELETE FROM patrons WHERE patronid=@patronid", new { patronid = key });

                List<long> booksRented = conn.Query<long>("SELECT bookid FROM books WHERE books.patronid = @patronid", new { patronid = key }).ToList();

                if (booksRented.Count() != 0)
                {
                    patron.patronid = -1;
                    patron.checkedoutBooks = new List<Book>(booksRented.Count());
                    return patron;
                }
                dbConnection.Close();
            }

            return patron;
        }

        public Patron Update(Patron myPatron)
        {
            using (IDbConnection dbConnection = conn)
            {
                dbConnection.Open();
                dbConnection.Query("UPDATE patrons SET fname = @fname,   lname = @lname, phone=@phone, emailid=@emailid WHERE patrons.patronid = @patronid", myPatron);
                dbConnection.Close();
            }
            return myPatron;
        }

        public Patron UpdateCheckOut(Patron myPatron)
        {
            Patron temp = null;
            using (IDbConnection dbConnection = conn)
            {
                List<long> booksIDToCheckout = new List<long>();
                List<Book> booksObjects = new List<Book>();
                //Collect valid bookobjects and bookids
                if (myPatron.checkedoutBooks.Count() != 0)
                {
                    foreach (Book book in myPatron.checkedoutBooks)
                    {
                        Book bookObject = conn.Query<Book>("select * from books where books.bookid=" + book.bookid).FirstOrDefault();

                        //if book exists, then only perform operation
                        if (bookObject != null)
                        {
                            booksIDToCheckout.Add(book.bookid);
                            booksObjects.Add(bookObject);

                            //if book is not available for check out
                            if (bookObject.checkedoutstatus.Equals("true"))
                            {
                                temp = new Patron();
                                temp.patronid = -1;
                                temp.fname = bookObject.bookid + "";

                            }

                        }
                    }
                }
                
                dbConnection.Open();
                List<Book> modifiedCheckedOutBooks = new List<Book>();
                foreach (Book book in booksObjects)
                {
                    //mark each book as checked out
                    dbConnection.Query("UPDATE books SET checkedoutstatus=" + true + ", patronid=" + myPatron.patronid + ", duedate='"+ DateTime.Now.AddDays(lendingPeriod)+"' WHERE books.bookid = " + book.bookid);

                }
                
                myPatron = FindByID(myPatron.patronid);
                dbConnection.Close();
            }

            if (temp != null)
            {
                return temp;
            }
            return myPatron;
        }


        public Patron UpdateReturn(Patron myPatron)
        {
            Patron temp = null;
            using (IDbConnection dbConnection = conn)
            {
                List<long> booksIDToReturn = new List<long>();
                List<Book> booksObjects = new List<Book>();

                if (myPatron.checkedoutBooks.Count() != 0)
                {
                    foreach (Book book in myPatron.checkedoutBooks)
                    {
                        Book bookObject = conn.Query<Book>("select * from books where books.bookid=" + book.bookid).FirstOrDefault();

                        //if book exists, then only perform operation
                        if (bookObject != null)
                        {
                            booksIDToReturn.Add(book.bookid);
                            booksObjects.Add(bookObject);

                            //if book is not available for returning
                            if (bookObject.checkedoutstatus.Equals("false"))
                            {
                                temp = new Patron();
                                temp.patronid = -1;
                                temp.fname = bookObject.bookid + "";
                            }
                        }
                    }
                }

                dbConnection.Open();
                List<Book> modifiedCheckedOutBooks = new List<Book>();
                foreach (Book book in booksObjects)
                {
                    Console.WriteLine("I a here book.bookid " + book.bookid);
                    dbConnection.Query("UPDATE books SET checkedoutstatus=" + false + ", patronid= default, duedate=default WHERE bookid = " + book.bookid);
                    book.patron = null;
                }
                myPatron = FindByID(myPatron.patronid);
                dbConnection.Close();
            }

            if (temp != null)
            {
                return temp;
            }
            return myPatron;
        }
    }
}
