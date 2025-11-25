# HTTP Requests & Regex

Unity uses HttpClient to fetch the teacher overview page from HTL Salzburg, extract all /lehrerinnen-details/*.html links, and asynchronously load each individual detail page. The HTML content is then analyzed with regular expressions to automatically extract the room number, office hour, and email address. The project demonstrates the use of async/await, HTTP GET requests, Regex-based text parsing, and basic HTML processing inside Unity.
