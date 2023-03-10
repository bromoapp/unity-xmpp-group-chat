/*
 * xpnet is a deriviative of James Clark's XP parser.
 * See copying.txt for more info.
 */
using System.Collections.Generic;

namespace Xmpp.Xml.Xpnet
{
    /// <summary>
    /// Namespace stack.
    /// </summary>
    public class NamespaceStack
    {
        private readonly Stack<Dictionary<string, string>> stack = new Stack<Dictionary<string, string>>();
        
        /// <summary>
        /// Create a new stack, primed with xmlns and xml as prefixes.
        /// </summary>
        public NamespaceStack()
        {
            Push();
            AddNamespace("xmlns", "http://www.w3.org/2000/xmlns/");
            AddNamespace("xml", "http://www.w3.org/XML/1998/namespace");
        }
        
        /// <summary>
        /// Declare a new scope, typically at the start of each element
        /// </summary>
        public void Push()
        {
            stack.Push(new Dictionary<string, string>());
        }

        /// <summary>
        /// Pop the current scope off the stack.  Typically at the end of each element.
        /// </summary>
        public void Pop()
        {
            stack.Pop();
        }

        /// <summary>
        /// Add a namespace to the current scope.
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="uri"></param>
        public void AddNamespace(string prefix, string uri)
        {
            stack.Peek().Add(prefix, uri);
        }

        /// <summary>
        /// Lookup a prefix to find a namespace.  Searches down the stack, starting at the current scope.
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public string LookupNamespace(string prefix)
        {
            foreach (Dictionary<string, string> ht in stack)
            {
                if ((ht.Count > 0) && (ht.ContainsKey(prefix)))
                    return ht[prefix];
            }
            return "";
        }

        /// <summary>
        /// The current default namespace.
        /// </summary>
        public string DefaultNamespace
        {
            get { return LookupNamespace(string.Empty); }
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
		public void Clear()
		{			
#if !CF
		    stack.Clear();
#else
			while (m_stack.Count > 0)
			    m_stack.Pop();
#endif
		}
    }
}