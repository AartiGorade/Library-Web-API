using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;  
using Newtonsoft.Json;

namespace Library.Models
{
    public class Book : BaseEntity
    {
        [Key]
        public long bookid { get; set; }
        public string title { get; set; }
        public DateTime publisheddate { get; set; }
        public string checkedoutstatus { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime duedate { get; set; }
        public Patron patron { get; set; }
        public MyLibrary myLibrary { get; set; } = new MyLibrary();
        public IEnumerable<BookAuthor> bookauthor { get; set; } = new List<BookAuthor>();

        public override string ToString()
        {
            return "My book value is " + bookid + "  " + title + "  " + publisheddate + "  " + checkedoutstatus + " " + myLibrary + "  " + bookauthor.ToString();
        }
    }

    public class Author : BaseEntity
    {
        [Key]
        public long authorid { get; set; }
        public string fname { get; set; }
        [Required]
        public string lname { get; set; }
        public IEnumerable<BookAuthor> bookauthor { get; set; } = new List<BookAuthor>();

        public override string ToString()
        {
            return "My Author value is " + authorid + "  " + fname + "  " + lname + "  " + bookauthor.ToString();
        }
    }

    public class BookAuthor : BaseEntity
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long bookid { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long authorid { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Book book { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Author author { get; set; }

        public override string ToString()
        {
            return "My BookAuthor value is " + bookid + "  " + book.ToString() + "  " + authorid + " " + author.ToString();
        }

    }


    public class MyLibrary : BaseEntity
    {
        public long libraryid { get; set; }
        public String libraryname { get; set; }
        public String libraryaddress { get; set; }
        public String libraryphone { get; set; }
        public IEnumerable<Book> myBooks { get; set; } = new List<Book>();

        public override string ToString()
        {
            return "My patron value is " + libraryid + "  " + libraryname + "  " + libraryaddress + "  " + libraryphone;
        }
    }

    public class Patron : BaseEntity
    {
        public long patronid { get; set; }
        public String fname { get; set; }
        public String lname { get; set; }
        public String emailid { get; set; }
        public String phone { get; set; }

        public IEnumerable<Book> checkedoutBooks { get; set; } = new List<Book>();

        public override string ToString()
        {
            return "My Patron value is " + patronid + "  " + fname + "  " + lname + "  " + emailid + " " + phone;
        }
    }



}