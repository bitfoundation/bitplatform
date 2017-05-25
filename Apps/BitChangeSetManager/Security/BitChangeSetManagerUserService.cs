using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Model;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services.Default;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Owin.Exceptions;

namespace BitChangeSetManager.Security
{
    public class BitChangeSetManagerUserService : UserServiceBase
    {
        private readonly IDependencyManager _dependencyManager;

        public BitChangeSetManagerUserService(IDependencyManager dependencyManager)
        {
            _dependencyManager = dependencyManager;
        }

        public override async Task AuthenticateLocalAsync(LocalAuthenticationContext context)
        {
            try
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

                    for (int i = 0; i < data.Length; i++)
                    {
                        sBuilder.Append(data[i].ToString("x2"));
                    }

                    password = sBuilder.ToString();
                }

                username = username.ToLower();

                User user = null;

                using (IDependencyResolver resolver = _dependencyManager.CreateChildDependencyResolver())
                {
                    IBitChangeSetManagerRepository<User> usersRepository = resolver.Resolve<IBitChangeSetManagerRepository<User>>();

                    user = usersRepository.GetAll()
                         .SingleOrDefault(u => u.UserName.ToLower() == username && u.Password == password);
                }

                if (user == null)
                    throw new InvalidOperationException("LoginFailed");

                string userId = user.Id.ToString();

                List<Claim> claims = new List<Claim>
                {
                    new Claim("sub",userId),
                    new Claim("primary_sid", userId),
                    new Claim("upn", userId),
                    new Claim("name", userId),
                    new Claim("given_name", userId)
                };

                AuthenticateResult result = new AuthenticateResult(userId, userId,
                    claims,
                    authenticationMethod: "custom");

                context.AuthenticateResult = result;

                await base.AuthenticateLocalAsync(context);
            }
            catch
            {
                AuthenticateResult result = new AuthenticateResult("LoginFailed");

                context.AuthenticateResult = result;
            }
        }

        public override async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            User user = null;

            using (IDependencyResolver resolver = _dependencyManager.CreateChildDependencyResolver())
            {
                IBitChangeSetManagerRepository<User> usersRepository = resolver.Resolve<IBitChangeSetManagerRepository<User>>();

                Guid userId = Guid.Parse(context.Subject.Identity.Name);

                user = usersRepository.GetAll()
                     .SingleOrDefault(u => u.Id == userId);
            }

            if (user != null)
            {
                string userId = user.Id.ToString();

                List<Claim> claims = new List<Claim>
                {
                    new Claim("sub", userId),
                    new Claim("primary_sid", userId),
                    new Claim("upn", userId),
                    new Claim("name", userId),
                    new Claim("given_name", userId)
                };

                context.IssuedClaims = claims;

                await base.GetProfileDataAsync(context);
            }
            else
            {
                throw new ResourceNotFoundException("User could not be found");
            }
        }

        public override async Task IsActiveAsync(IsActiveContext context)
        {
            User user = null;

            using (IDependencyResolver resolver = _dependencyManager.CreateChildDependencyResolver())
            {
                IBitChangeSetManagerRepository<User> usersRepository = resolver.Resolve<IBitChangeSetManagerRepository<User>>();

                Guid userId = Guid.Parse(context.Subject.Identity.Name);

                user = usersRepository.GetAll()
                     .SingleOrDefault(u => u.Id == userId);
            }

            context.IsActive = user != null;

            await base.IsActiveAsync(context);
        }
    }
}