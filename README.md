# MailTest
MailTest was written in C# and utilizes the .Net Framework.

MailTest has simple testers that open POP3 and SMTP connections waits for the proper response and acts accordingly - reporting any errors along the way.

Due to the requirements to connect using ssl authentication, there is an option to use System.Diagnostics, which provides the .Net diagnostic trace of System.Net, System.Net.Sockets, and the System.Net.Pop3 library.

### SMTP Testing
When performing SMTP Test on incoming mail, it is import to test all of the relay points that accepts the message.  EXAMPLE: Mail Relays, Bridgeheads, Exchange Server.

Multiple SMTP servers and TO addresses can be entered seperated by semi-colons ;  You can also use the MX LOOKUP tab to perform a lookup and add individual smtp servers to test.  Then just right click and select the SMTP Test All option.

When performing SMTP Test on outgoing mail, perform an nslookup to get the different mail exchangers and test each mail exchanger starting at the highest priority (lowest MX Preference).
