﻿/*

This file is part of the iText (R) project.
Copyright (c) 1998-2017 iText Group NV
Authors: Bruno Lowagie, Paulo Soares, et al.

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License version 3
as published by the Free Software Foundation with the addition of the
following permission added to Section 15 as permitted in Section 7(a):
FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
ITEXT GROUP. ITEXT GROUP DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
OF THIRD PARTY RIGHTS

This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
or FITNESS FOR A PARTICULAR PURPOSE.
See the GNU Affero General Public License for more details.
You should have received a copy of the GNU Affero General Public License
along with this program; if not, see http://www.gnu.org/licenses or write to
the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
Boston, MA, 02110-1301 USA, or download the license from the following URL:
http://itextpdf.com/terms-of-use/

The interactive user interfaces in modified source and object code versions
of this program must display Appropriate Legal Notices, as required under
Section 5 of the GNU Affero General Public License.

In accordance with Section 7(b) of the GNU Affero General Public License,
a covered work must retain the producer line in every PDF that is created
or manipulated using iText.

You can be released from the requirements of the license by purchasing
a commercial license. Buying such a license is mandatory as soon as you
develop commercial activities involving the iText software without
disclosing the source code of your own applications.
These activities include: offering paid services to customers as an ASP,
serving PDFs on the fly in a web application, shipping iText with a closed
source product.

For more information, please contact iText Software Corp. at this
address: sales@itextpdf.com
*/
using System;
using System.Linq;
using System.Text.RegularExpressions;
using iText.IO.Log;
using iText.Test.Attributes;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace iText.Test {
    [AttributeUsage(AttributeTargets.Class)]
    public class LogListener : TestActionAttribute {
        private MemoryAppender appender;

        static LogListener() {
            ITextMemoryAppender memoryAppender = new ITextMemoryAppender();
            memoryAppender.Layout = new PatternLayout("%message");
            ILoggerRepository repo = LogManager.GetRepository(typeof(LogListener).GetAssembly());
            BasicConfigurator.Configure(repo, memoryAppender);
        }

        public override void BeforeTest(ITest testDetails) {
            Init();
        }

        public override void AfterTest(ITest testDetails) {
            CheckLogMessages(testDetails);
        }

        public override ActionTargets Targets {
            get { return ActionTargets.Test; }
        }

        private void CheckLogMessages(ITest testDetails) {
            int checkedMessages = 0;
            LogMessageAttribute[] attributes = testDetails.Method.GetCustomAttributes<LogMessageAttribute>(true);
            if (attributes.Length == 0) {
                attributes = testDetails.Fixture.GetType().GetCustomAttributes(typeof(LogMessageAttribute), true)
                    .Select(attr => (LogMessageAttribute) attr).ToArray();
            }
            if (attributes.Length > 0) {
                for (int i = 0; i < attributes.Length; i++) {
                    LogMessageAttribute logMessage = attributes[i];
                    int foundCount = Contains(logMessage.GetMessageTemplate());
                    if (foundCount != logMessage.Count && !logMessage.Ignore) {
                        Assert.Fail(
                            "{0} Expected to find {1}, but found {2} messages with the following content: \"{3}\"",
                            testDetails.FullName, logMessage.Count, foundCount, logMessage.GetMessageTemplate());
                    } else {
                        checkedMessages += foundCount;
                    }
                }
            }

            if (GetSize() > checkedMessages) {
                Assert.Fail("{0}: The test does not check the message logging - {1} messages",
                    testDetails.FullName,
                    GetSize() - checkedMessages);
            }
        }

        /*
        * compare  parametrized message with  base template, for example:
        *  "Hello fox1 , World  fox2 !" with "Hello {0} , World {1} !"
        * */

        private bool EqualsMessageByTemplate(string message, string template) {
            if (template.Contains("{") && template.Contains("}")) {
                String templateWithoutParameters = Regex.Replace(template.Replace("''", "'"), "\\{[0-9]+?\\}", "(.)*?");
                return Regex.IsMatch(message, templateWithoutParameters, RegexOptions.Singleline);
            } else {
                return message.Contains(template);
            }
        }

        private int Contains(String loggingStatement) {
            LoggingEvent[] eventList = appender.GetEvents();
            int index = 0;
            for (int i = 0; i < eventList.Length; i++) {
                if (EqualsMessageByTemplate(eventList[i].RenderedMessage, loggingStatement)) {
                    index++;
                }
            }
            return index;
        }

        private void Init() {
            ILoggerFactory iLog = new Log4NetLoggerFactory();
            LoggerFactory.BindFactory(iLog);
            //LogManager.GetRepository() calls Assembly.GetCallingAssembly() so it will always be current (this) assembly
            IAppender[] iAppenders = LogManager.GetRepository(typeof(LogListener).GetAssembly()).GetAppenders();
            appender = iAppenders[0] as MemoryAppender;
            appender.Clear();
        }

        private int GetSize() {
            return appender.GetEvents().Length;
        }
    }
}
