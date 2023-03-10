using System;

using Xmpp.Xml.Dom;

using Xmpp.protocol.extensions.filetransfer;
using Xmpp.protocol.extensions.featureneg;

namespace Xmpp.protocol.extensions.si
{
	/// <summary>
	/// JEP-0095: Stream Initiation.
	/// This JEP defines a protocol for initiating a stream (with meta information) between any two Jabber entities.
	/// </summary>
	public class SI : Element
	{
		public SI()
		{
			this.TagName	= "si";
			this.Namespace	= Uri.SI;
		}

		//id='a0'
		//mime-type='text/plain'

		/// <summary>
		/// The "id" attribute is an opaque identifier. 
		/// This attribute MUST be present on type='set', and MUST be a valid string. 
		/// This SHOULD NOT be sent back on type='result', since the &lt;iq/&gt; "id" attribute provides the only context needed.
		/// This value is generated by the Sender, and the same value MUST be used throughout a session when talking to the Receiver.
		/// </summary>
		public string Id
		{
			get { return GetAttribute("id"); }
			set { SetAttribute("id", value); }
		}

		/// <summary>
		/// The "mime-type" attribute identifies the MIME-type for the data across the stream.
		/// This attribute MUST be a valid MIME-type as registered with the Internet Assigned Numbers Authority (IANA) [3] 
		/// (specifically, as listed at "http://www.iana.org/assignments/media-types"). 
		/// During negotiation, this attribute SHOULD be present, and is otherwise not required. 
		/// If not included during negotiation, its value is assumed to be "binary/octect-stream".
		/// </summary>
		public string MimeType
		{
			get { return GetAttribute("mime-type"); }
			set { SetAttribute("mime-type", value); }
		}

		/// <summary>
		/// The "profile" attribute defines the SI profile in use. This value MUST be present during negotiation,
		/// and is the namespace of the profile to use.
		/// </summary>
		public string Profile
		{
			get { return GetAttribute("profile"); }
			set { SetAttribute("profile", value); }
		}


        /// <summary>
        /// the FeatureNeg Element 
        /// </summary>
        public FeatureNeg FeatureNeg
        {
            get
            {
                return SelectSingleElement(typeof(FeatureNeg)) as FeatureNeg;
            }
            set
            {
                if (HasTag(typeof(FeatureNeg)))
                    RemoveTag(typeof(FeatureNeg));

                if (value != null)
                    this.AddChild(value);
            }
        }

        /// <summary>
        /// the File Element
        /// </summary>
        public File File
        {
            get
            {
                return SelectSingleElement(typeof(File)) as File;
            }
            set
            {
                if (HasTag(typeof(File)))
                    RemoveTag(typeof(File));

                if (value != null)
                    this.AddChild(value);
            }
        }

	}
}
