using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NLog;
using NLog.Fluent;

namespace JiraTFS
{
    public static class LogExtensions
    {
	    

	    public class LogMessage
	    {
	        public Site Site { get; set; }
            public Operation Operation { get; set; }
            public Artifact Artifact { get; set; }
            public string ArtifactId { get; set; }
            public string OwnerId { get; set; }
            public string PartnerId { get; set; }
            public string Status { get; set; }
	    }


        public static void WriteInfo(this Logger logger, Func<LogBuilder, LogBuilder> what)
        {
            what(logger.Info()).Write();
        }

        public static void WriteError(this Logger logger, Func<LogBuilder, LogBuilder> what)
        {
            what(logger.Error()).Write();
        }


        public static LogBuilder JMessage(this LogBuilder builder, Site site, Operation operation, Artifact artifact,
            string artifactId, string ownerId = null, string partnerId = null, string status = null)
        {
            var message = new LogMessage
            {
                Site = site,
                Operation = operation,
                Artifact = artifact,
                ArtifactId = artifactId,
                OwnerId = ownerId,
                PartnerId = partnerId
            };

            var msg = JsonConvert.SerializeObject(message, Formatting.None, JsonSettings);
            return builder.Message(msg);
        }

        

        private static JsonSerializerSettings JsonSettings
        {
            get
            {
                return new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    Converters = new JsonConverter[] {new StringEnumConverter(),}
                };
            }
        }
    }

    public enum Artifact
    {
        Attachment,
        WorkItem,
		Issue,
        Comment,
        Query
    }

    public enum Operation
    {
        Create,
        Changed,
        Removed,
        Sended,
        Reading,
        Success,
        Readed
    }
}
