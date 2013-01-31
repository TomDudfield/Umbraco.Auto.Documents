using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Umbraco.Core.Events;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core;

namespace AutoDocuments
{
    public class AutoDocumentsApplicationEventHandler : IApplicationEventHandler
    {
        private AutoDocuments _autoDocuments;

        public void OnApplicationInitialized(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
        }

        public void OnApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
        }

        public void OnApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            var itemDocumentTypes = ConfigurationManager.AppSettings["AutoDocuments:ItemDocumentTypes"];
            var dateDocumentType = ConfigurationManager.AppSettings["AutoDocuments:DateDocumentType"];
            var itemDatePropertyAlias = ConfigurationManager.AppSettings["AutoDocuments:ItemDatePropertyAlias"];

            List<string> itemDocTypes = null;
            bool createDayDocuments = false;

            if (!string.IsNullOrEmpty(itemDocumentTypes))
                itemDocTypes = itemDocumentTypes.Split(',').ToList();

            string createDayDocumentsSetting = ConfigurationManager.AppSettings["AutoDocuments:CreateDayDocuments"];

            if (!string.IsNullOrEmpty(createDayDocumentsSetting))
                bool.TryParse(createDayDocumentsSetting, out createDayDocuments);

            if (itemDocTypes == null || itemDocTypes.Count == 0 || string.IsNullOrEmpty(dateDocumentType) || string.IsNullOrEmpty(itemDatePropertyAlias))
            {
                LogHelper.Debug<AutoDocumentsApplicationEventHandler>(string.Format("Auto Documents configuration invalid, ItemDocumentTypes:{0} DateDocumentType:{1} ItemDatePropertyAlias:{2}", itemDocumentTypes, dateDocumentType, itemDatePropertyAlias));
                return;
            }

            _autoDocuments = new AutoDocuments(itemDocTypes, itemDatePropertyAlias, dateDocumentType, createDayDocuments);

            ContentService.Created += ContentServiceCreated;
            ContentService.SendingToPublish += ContentServiceSendingToPublish;
        }
        
        private void ContentServiceCreated(IContentService sender, NewEventArgs<IContent> e)
        {
            _autoDocuments.SetDocumentDate(sender, e.Entity);
        }

        private void ContentServiceSendingToPublish(IContentService sender, SendToPublishEventArgs<IContent> e)
        {
            _autoDocuments.BeforeDocumentPublish(sender, e.Entity);
        }
    }
}
