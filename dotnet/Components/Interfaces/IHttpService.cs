using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace TeamHitori.Mulplay.Container.Web.Components.Interfaces
{
    public interface IHttpService
    {
        Task<T> UrlDeleteType<T>(string uri, int retries = 6) where T : class;
        Task<T> UrlGetType<T>(string uri, int retries = 6) where T : class;
        Task<T> UrlPostType<T>(string uri, string postContent, int retries = 6) where T : class;
    }

    public enum RequestorType
    {
        User,
        Client
    }
}
