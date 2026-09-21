# TechStore E-Commerce

TechStore is a simple ASP.NET Core MVC e-commerce project built with C# and .NET 8.

The project was created as a Computer Science portfolio project and demonstrates core web development concepts such as authentication, CRUD operations, validation, search, and responsive UI design.

## Features

- User registration and login
- Secure password hashing using ASP.NET Core `PasswordHasher`
- Cookie-based authentication
- Logout functionality
- Public product browsing
- Add products
- Edit products
- Delete products
- Product search
- Product filtering
- Product count
- Server-side validation
- Responsive Bootstrap interface
- Empty-state messages
- Success and error notifications

## Technologies Used

- C#
- ASP.NET Core MVC
- .NET 8
- Razor Views
- Bootstrap
- HTML5
- CSS3
- JavaScript
- ASP.NET Core Authentication

## Security

The project includes basic authentication security improvements:

- Passwords are stored as secure hashes instead of plain text
- Authentication cookies are used to maintain login sessions
- Product management actions require authentication
- Logout requests are protected with anti-forgery validation
- Sensitive credentials are not stored in `appsettings.json`

## Project Structure

```text
Controllers/
Models/
Views/
wwwroot/
Program.cs
appsettings.json
