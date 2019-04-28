using Bit.Core.Contracts;
using Bit.IdentityServer.Implementations;
using Bit.Owin.Exceptions;
using BitChangeSetManager.DataAccess.Contracts;
using BitChangeSetManager.Model;
using IdentityServer3.Core.Models;
using System;
using System.Data.Entity;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BitChangeSetManager.Security
{
    public class BitChangeSetManagerUserService : UserService
    {
        public virtual IBitChangeSetManagerRepository<User> UsersRepository { get; set; }

        public async override Task<BitJwtToken> LocalLogin(LocalAuthenticationContext context, CancellationToken cancellationToken)
        {
            string username = context.UserName;
            string password = context.Password;

            if (string.IsNullOrEmpty(username))
                throw new ArgumentException(nameof(username));

            if (string.IsNullOrEmpty(password))
                throw new ArgumentException(nameof(password));

            using (MD5 md5Hash = MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                StringBuilder sBuilder = new StringBuilder();

                foreach (byte d in data)
                {
                    sBuilder.Append(d.ToString("x2"));
                }

                password = sBuilder.ToString();
            }

            username = username.ToLower();

            User user = null;

            user = await (await UsersRepository.GetAllAsync(cancellationToken))
                 .SingleOrDefaultAsync(u => u.UserName.ToLower() == username && u.Password == password);

            if (user == null)
                throw new DomainLogicException("LoginFailed");

            return new BitJwtToken { UserId = user.Id.ToString() };
        }

        public async override Task<bool> UserIsActiveAsync(IsActiveContext context, BitJwtToken jwtToken, CancellationToken cancellationToken)
        {
            Guid userIdAsGuid = Guid.Parse(jwtToken.UserId);

            return await (await UsersRepository.GetAllAsync(cancellationToken))
                 .AnyAsync(u => u.Id == userIdAsGuid);
        }
    }
}