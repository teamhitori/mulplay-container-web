using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeamHitori.Mulplay.shared.storage;

namespace TeamHitori.Mulplay.Container.Web.Components
{
    public static class SessionExtensions
    {
        public static void SetObj<T>(this ISession session, string primaryName, T objIn) where T : class
        {
            var typeName = typeof(T).Name;
            var doc = objIn.ToJDoc();

            session.SetString($"{typeName}:{primaryName}", doc.content);
        }

        public static T GetObj<T>(this ISession session, string primaryName) where T : class
        {
            var typeName = typeof(T).Name;
            var contents = session.GetString($"{typeName}:{primaryName}");

            var res = contents.GetObject<T>();

            return res;
        }

        public static IEnumerable<T> GetAllOfType<T>(this ISession session) where T : class
        {
            var typeName = typeof(T).Name;
            foreach (var key in session.Keys)
            {
                if (key.StartsWith($"{typeName}:"))
                {
                    var contents = session.GetString(key);
                    var res = contents.GetObject<T>();

                    yield return res;
                }
            }
        }
    }
}
