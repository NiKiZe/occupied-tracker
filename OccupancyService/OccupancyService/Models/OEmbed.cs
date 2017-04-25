using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace OccupancyService.Models
{
    [DataContract(Name = "oembed")]
    [XmlRoot("oembed")]
    public class OEmbed
    {
        /// <summary>
        /// Required. The resource type. Valid values, along with value-specific parameters, are described below.
        /// photo - This type is used for representing static photos.
        /// video - This type is used for representing playable videos.
        /// link - Responses of this type allow a provider to return any generic embed data (such as title and author_name), without providing either the url or html parameters. The consumer may then link to the resource, using the URL specified in the original request.
        /// rich - This type is used for rich HTML content that does not fall under one of the other categories
        /// </summary>
        [DataMember(Name = "type", EmitDefaultValue = false)]
        [XmlElement("type")]
        public string Type { get; set; }

        /// <summary>
        /// The oEmbed version number. This must be 1.0.
        /// </summary>
        [DataMember(Name = "version", EmitDefaultValue = false)]
        [XmlElement("version")]
        public string Version { get; set; }

        /// <summary>
        /// Optional. A text title, describing the resource.
        /// </summary>
        [DataMember(Name = "title", EmitDefaultValue = false)]
        [XmlElement("title")]
        public string Title { get; set; }

        /// <summary>
        /// Optional. The name of the author/owner of the resource.
        /// </summary>
        [DataMember(Name = "author_name", EmitDefaultValue = false)]
        [XmlElement("author_name")]
        public string AuthorName { get; set; }

        /// <summary>
        /// Optional. A URL for the author/owner of the resource.
        /// </summary>
        [DataMember(Name = "author_url", EmitDefaultValue = false)]
        [XmlElement("author_url")]
        public string AuthorUrl { get; set; }

        /// <summary>
        /// Optional. The name of the resource provider.
        /// </summary>
        [DataMember(Name = "provider_name", EmitDefaultValue = false)]
        [XmlElement("provider_name")]
        public string ProviderName { get; set; }

        /// <summary>
        /// Optional. The url of the resource provider.
        /// </summary>
        [DataMember(Name = "provider_url", EmitDefaultValue = false)]
        [XmlElement("provider_url")]
        public string ProviderUrl { get; set; }

        /// <summary>
        /// Optional. The suggested cache lifetime for this resource, in seconds. Consumers may choose to use this value or not.
        /// </summary>
        [DataMember(Name = "cache_age", EmitDefaultValue = false)]
        [XmlElement("cache_age")]
        public int CacheAge { get; set; }

        /// <summary>
        /// Optional. A URL to a thumbnail image representing the resource.
        /// The thumbnail must respect any maxwidth and maxheight parameters.
        /// If this parameter is present, thumbnail_width and thumbnail_height must also be present.
        /// </summary>
        [DataMember(Name = "thumbnail_url", EmitDefaultValue = false)]
        [XmlElement("thumbnail_url")]
        public string ThumbnailUrl { get; set; }

        /// <summary>
        /// Optional. The width of the optional thumbnail. If this parameter is present, thumbnail_url and thumbnail_height must also be present.
        /// </summary>
        [DataMember(Name = "thumbnail_width", EmitDefaultValue = false)]
        [XmlElement("thumbnail_width")]
        public string ThumbnailWidth { get; set; }

        /// <summary>
        /// Optional. The height of the optional thumbnail. If this parameter is present, thumbnail_url and thumbnail_width must also be present.
        /// </summary>
        [DataMember(Name = "thumbnail_height", EmitDefaultValue = false)]
        [XmlElement("thumbnail_height")]
        public string ThumbnailHeight { get; set; }

        /// <summary>
        /// For type=photo only. Required. The source URL of the image. Consumers should be able to insert this URL into an <img> element. Only HTTP and HTTPS URLs are valid.
        /// </summary>
        [DataMember(Name = "url", EmitDefaultValue = false)]
        [XmlElement("url")]
        public string Url { get; set; }

        /// <summary>
        /// For type=photo,video,rich only. Required. The width in pixels required.
        /// </summary>
        [DataMember(Name = "width", EmitDefaultValue = false)]
        [XmlElement("width")]
        public int? Width { get; set; }

        /// <summary>
        /// For type=photo,video,rich only. Required. The height in pixels required.
        /// </summary>
        [DataMember(Name = "height", EmitDefaultValue = false)]
        [XmlElement("height")]
        public int? Height { get; set; }

        /// <summary>
        /// For type=video,rich only. Required. The HTML required to display the resource. The HTML should have no padding or margins. Consumers may wish to load the HTML in an off-domain iframe to avoid XSS vulnerabilities.
        /// </summary>
        [DataMember(Name = "html", EmitDefaultValue = false)]
        [XmlElement("html")]
        public string Html { get; set; }
    }
}