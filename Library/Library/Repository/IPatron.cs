using Library.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.Repository
{
        public interface IPatron<T> where T : BaseEntity
        {
            T Add(T item);
            T Remove(long id);
            T Update(T item);
            T FindByID(long id);
            IEnumerable<T> FindAll();
        }
}
