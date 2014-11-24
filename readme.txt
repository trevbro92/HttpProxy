Trevor Broaddus, Brad Wright
Networking
Dr. Yi Gu
HttpProxy Project

About:
	The HttpProxy is a proxy server that handles HTTP requests from the browser of your choice, and directs them to their intended web server, 
with the only caveat being it does not support SSL ?  It does provide a very simple UI that displays relevant information (relevant at least to a certain
couple of developers) about requests made from connected clients, Exception information if/when they are thrown, and a history of used socket addresses as 
well as web server addresses. 

How To:
	Begin by starting the application.  Select the IP Address that you wish to use as the listening address for the HttpProxy.  Once selected, 
input the IP into your browsers proxy settings.  Press the shiny Start button and begin browsing.  A live feed of requests and exceptions are displayed 
as well as a list of client addresses.  The number in the window next to the Application Title represents the number of clients sending requests to the 
proxy, and will fluctuate depending on the websites requested.  Once you done, clicking the Terminate button will end all connections and essentially 
reset the proxy until the Start button is pressed again.  The Client History will remain throughout the session until the Application Window is closed.

First 6th line of GET request to www.google.com:
     "Cookie: PREF=ID=4e35f249c84c0798:U=aff364e61dbfd3b3:FF="