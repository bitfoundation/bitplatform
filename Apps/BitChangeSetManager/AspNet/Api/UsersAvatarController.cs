using Bit.Core.Contracts;
using Bit.Owin.Exceptions;
using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Model;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace BitChangeSetManager.Api
{
    public class UsersAvatarController : ApiController
    {
        public IBitChangeSetManagerRepository<User> UsersRepository { get; set; }

        public IUserInformationProvider UserInformationProvider { get; set; }

        [HttpGet]
        [Route("user/get-current-user-avatar")]
        public async Task<HttpResponseMessage> GetUserAvatar(CancellationToken cancellationToken)
        {
            Guid currentUserId = Guid.Parse(UserInformationProvider.GetCurrentUserId());

            byte[] userAvatar = await (await UsersRepository.GetAllAsync(cancellationToken))
                .Where(u => u.Id == currentUserId)
                .Select(u => u.AvatarImage)
                .SingleOrDefaultAsync(cancellationToken);

            if (userAvatar == null)
                throw new ResourceNotFoundException();

            ByteArrayContent content = new ByteArrayContent(userAvatar);
            content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            
            return new HttpResponseMessage
            {
                Content = content,
                StatusCode = System.Net.HttpStatusCode.OK
            };
        }
    }
}