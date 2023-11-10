using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MicroRabbit.Banking.Data.Context;
using MicroRabbit.Banking.Domain.Interfaces;
using MicroRabbit.Banking.Domain.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MicroRabbit.Banking.Data.Repository
{
    public class AccountRepository : IAccountRepository
    {
        private BankingDBContext _context;

        public AccountRepository(BankingDBContext context)
        {
            this._context = context;
        }

        public IEnumerable<Account> GetAccounts()
        {
            return this._context.Accounts;
        }
    }
}