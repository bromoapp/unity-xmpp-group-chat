using System;

using Xmpp.protocol.client;

namespace Xmpp.protocol.client
{
	/// <summary>
	/// Helper class for managing presence and subscriptions
	/// </summary>
	public class PresenceManager
	{
		private XmppClientConnection	m_connection	= null;

		public PresenceManager(XmppClientConnection con)
		{
            m_connection = con;			
		}
		        
		/// <summary>
        /// Subscribe to a contact
		/// </summary>
		/// <param name="to">Bare Jid of the rosteritem we want to subscribe</param>
		public void Subscribe(Jid to)
		{
			// <presence to='contact@example.org' type='subscribe'/>
			Presence pres = new Presence();
			pres.Type = PresenceType.subscribe;
			pres.To = to;

			m_connection.Send(pres);
		}
        
        /// <summary>        
        /// Subscribe to a contact
        /// </summary>        
        /// <param name="to">Bare Jid of the rosteritem we want to subscribe</param>
        /// <param name="message">a message which normally contains the reason why we want to subscibe to this contact</param>
        public void Subscribe(Jid to, string message)
        {
            Presence pres = new Presence();
            pres.Type = PresenceType.subscribe;
            pres.To = to;
            pres.Status = message;

            m_connection.Send(pres);
        }
        
        /// <summary>
        /// Unsubscribe from a contact
        /// </summary>
        /// <param name="to">Bare Jid of the rosteritem we want to unsubscribe</param>
		public void Unsubscribe(Jid to)
		{
			// <presence to='contact@example.org' type='subscribe'/>
			Presence pres = new Presence();
			pres.Type = PresenceType.unsubscribe;
			pres.To = to;

			m_connection.Send(pres);
		}

		//Example: Approving a subscription request:
		//<presence to='romeo@example.net' type='subscribed'/>
		
        /// <summary>
        /// Approve a subscription request
        /// </summary>
        /// <param name="to">Bare Jid to approve</param>
        public void ApproveSubscriptionRequest(Jid to)
		{
			// <presence to='contact@example.org' type='subscribe'/>
			Presence pres = new Presence();
			pres.Type = PresenceType.subscribed;
			pres.To = to;

			m_connection.Send(pres);
		}

		//Example: Refusing a presence subscription request:
		//<presence to='romeo@example.net' type='unsubscribed'/>
		
        /// <summary>
        /// Refuse  subscription request
        /// </summary>
        /// <param name="to">Bare Jid to approve</param>
        public void RefuseSubscriptionRequest(Jid to)
		{
			// <presence to='contact@example.org' type='subscribe'/>
			Presence pres = new Presence();
			pres.Type = PresenceType.unsubscribed;
			pres.To = to;

			m_connection.Send(pres);
		}
	}
}