// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
// using Umbraco.Cms.Core.Cache;
// using Umbraco.Cms.Core.Models.PublishedContent;
// using Umbraco.Cms.Core.Web;
// using Umbraco.Extensions;

// namespace Umbraco.Cms.Core.Services
// {
//     public class SettingsService : ISettingsService
//     {
//         private readonly IAppPolicyCache _runtimeCache;
//         private readonly IUmbracoContextFactory _contextFactory;

//         public SettingsService(IUmbracoContextFactory contextFactory, IAppPolicyCache runtimeCache)
//         {
//             _contextFactory = contextFactory;
//             _runtimeCache = runtimeCache;
//         }

//         public T Get<T>(IPublishedContent publishedContent, string alias, string? culture = null, T defaultValue = default(T))
//         {
//             try
//             {
//                 IPublishedContent? publishedContent2 = publishedContent?.Root();
//                 string cacheKey = "Setting_" + alias + "_" + publishedContent2?.Id + "_" + culture;
//                 T cacheItem = _runtimeCache.GetCacheItem<T>(cacheKey);
//                 if (cacheItem != null)
//                 {
//                     return cacheItem;
//                 }

//                 IPublishedContent firstSettingsNodeWithPropertyValue = GetFirstSettingsNodeWithPropertyValue(publishedContent2, alias);
//                 T value = firstSettingsNodeWithPropertyValue.Value(alias, culture, null, Fallback.ToDefaultValue, defaultValue);
//                     _runtimeCache.InsertCacheItem(cacheKey, () => value);
//                 return value;
                
//             }
//             catch
//             {
//                 return defaultValue;
//             }
//         }

//         private IPublishedContent? GetFirstSettingsNodeWithPropertyValue(IPublishedContent? site, string alias)
//         {
//             return site?.Children?.FirstOrDefault((IPublishedContent x) => x.IsDocumentType("siteSettings"))?.Children?.FirstOrDefault((IPublishedContent x) => x.HasProperty(alias));
//         }
//     }
// }
