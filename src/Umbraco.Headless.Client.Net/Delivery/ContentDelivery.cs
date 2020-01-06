using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Refit;
using Umbraco.Headless.Client.Net.Configuration;
using Umbraco.Headless.Client.Net.Delivery.Models;
using Umbraco.Headless.Client.Net.Serialization;

namespace Umbraco.Headless.Client.Net.Delivery
{
    internal class ContentDelivery : IContentDelivery
    {
        private readonly IHeadlessConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private ContentDeliveryEndpoints _service;

        public ContentDelivery(IHeadlessConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        private ContentDeliveryEndpoints Service =>
            _service ?? (_service = RestService.For<ContentDeliveryEndpoints>(_httpClient,
                new RefitSettings
                {
                    ContentSerializer = new JsonContentSerializer(new JsonSerializerSettings()
                    {
                        Converters =
                        {
                            new ContentConverter(
                                _configuration.ContentModelTypes.ToDictionary(GetAliasFromClassName)
                            )
                        }
                    })
                }));

        public async Task<IEnumerable<Content>> GetRoot(string culture)
        {
            var root = await Service.GetRoot(_configuration.ProjectAlias, culture);
            return root.Content.Items;
        }

        public async Task<IEnumerable<T>> GetRoot<T>(string culture) where T : IContent
        {
            var contentType = GetAliasFromClassName<T>();

            var service = RestService.For<TypedContentRootDeliveryEndpoints<T>>(_httpClient);
            var root = await service.GetRoot(_configuration.ProjectAlias, culture, contentType);
            return root.Content.Items;
        }

        public async Task<Content> GetById(Guid id, string culture, int depth)
        {
            var content = await Service.GetById(_configuration.ProjectAlias, culture, id, depth);
            return content;
        }

        public async Task<T> GetById<T>(Guid id, string culture, int depth) where T : IContent
        {
            var contentType = GetAliasFromClassName<T>();

            var service = RestService.For<TypedContentDeliveryEndpoints<T>>(_httpClient);
            var content = await service.GetById(_configuration.ProjectAlias, culture, id, contentType, depth);
            return content;
        }

        public async Task<Content> GetByUrl(string url, string culture, int depth)
        {
            var content = await Service.GetByUrl(_configuration.ProjectAlias, culture, url, depth);
            return content;
        }

        public async Task<T> GetByUrl<T>(string url, string culture, int depth) where T : IContent
        {
            var contentType = GetAliasFromClassName<T>();

            var service = RestService.For<TypedContentDeliveryEndpoints<T>>(_httpClient);
            var content = await service.GetByUrl(_configuration.ProjectAlias, culture, url, contentType, depth);
            return content;
        }

        public async Task<PagedContent> GetChildren(Guid id, string culture, int page, int pageSize)
        {
            var content = await Service.GetChildren(_configuration.ProjectAlias, culture, id, page, pageSize);
            return content;
        }

        public async Task<PagedContent<T>> GetChildren<T>(Guid id, string culture, int page, int pageSize) where T : IContent
        {
            var contentType = GetAliasFromClassName<T>();

            var service = RestService.For<TypedPagedContentDeliveryEndpoints<T>>(_httpClient);
            var content = await service.GetChildren(_configuration.ProjectAlias, culture, id, contentType, page, pageSize);
            return content;
        }

        public async Task<PagedContent> GetDescendants(Guid id, string culture, int page, int pageSize)
        {
            var content = await Service.GetDescendants(_configuration.ProjectAlias, culture, id, page, pageSize);
            return content;
        }

        public async Task<PagedContent<T>> GetDescendants<T>(Guid id, string culture, int page, int pageSize) where T : IContent
        {
            var contentType = GetAliasFromClassName<T>();

            var service = RestService.For<TypedPagedContentDeliveryEndpoints<T>>(_httpClient);
            var content = await service.GetDescendants(_configuration.ProjectAlias, culture, id, contentType, page, pageSize);
            return content;
        }

        public async Task<IEnumerable<Content>> GetAncestors(Guid id, string culture)
        {
            var root = await Service.GetAncestors(_configuration.ProjectAlias, culture, id);
            return root.Content.Items;
        }

        public async Task<IEnumerable<T>> GetAncestors<T>(Guid id, string culture) where T : IContent
        {
            var contentType = GetAliasFromClassName<T>();

            var service = RestService.For<TypedContentRootDeliveryEndpoints<T>>(_httpClient);
            var root = await service.GetAncestors(_configuration.ProjectAlias, culture, id, contentType);
            return root.Content.Items;
        }

        public async Task<PagedContent> GetByType(string contentType, string culture = null, int page = 1, int pageSize = 10)
        {
            var content = await Service.GetByType(_configuration.ProjectAlias, culture, contentType, page, pageSize);
            return content;
        }

        public async Task<PagedContent<T>> GetByType<T>(string culture = null, int page = 1, int pageSize = 10) where T : IContent
        {
            var contentType = GetAliasFromClassName<T>();
            return await GetByTypeAlias<T>(contentType, culture, page, pageSize);
        }

        public async Task<PagedContent<T>> GetByTypeAlias<T>(string contentTypeAlias, string culture = null, int page = 1, int pageSize = 10) where T : IContent
        {
            var service = RestService.For<TypedPagedContentDeliveryEndpoints<T>>(_httpClient);
            var content = await service.GetByType(_configuration.ProjectAlias, culture, contentTypeAlias, page, pageSize);
            return content;
        }

        public async Task<PagedContent> Filter(ContentFilter filter, string culture = null, int page = 1, int pageSize = 10)
        {
            if(filter == null || filter.Properties.Length == 0)
                throw new ArgumentException("ContentFilter should contain at least one property to filter on");

            var service = RestService.For<ContentDeliveryEndpoints>(_httpClient);
            var content = await service.Filter(_configuration.ProjectAlias, culture, filter, page, pageSize);
            return content;
        }

        public async Task<PagedContent> Search(string term, string culture = null, int page = 1, int pageSize = 10)
        {
            var content = await Service.Search(_configuration.ProjectAlias, culture, term, page, pageSize);
            return content;
        }

        private static string GetAliasFromClassName<T>() => GetAliasFromClassName(typeof(T));

        private static string GetAliasFromClassName(Type type)
        {
            if (type.GetCustomAttribute(typeof(ContentModelAttribute)) is ContentModelAttribute attr)
                return attr.ContentTypeAlias;

            var className = type.Name;
            if (className.IndexOf("Model", StringComparison.Ordinal) > -1)
            {
                className = className.Substring(0, className.IndexOf("Model", StringComparison.Ordinal));
            }

            // test for default casing
            if (className.Length > 1)
                className = $"{className.Substring(0, 1).ToLower()}{className.Substring(1)}";

            return className;
        }
    }
}
