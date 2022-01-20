Create database Tatvasoft_trainee

use Tatvasoft_trainee
go

Create Schema Helperland

go

Create Table Helperland.Usertype
(
TypeID int identity (1,1) primary key,
Type nvarchar (50) 
)

Create Table Helperland.Users
(
UserID int identity (1,1) primary key,
TypeID int not null foreign key references Helperland.Usertype (TypeID),
FirstName nvarchar(50) not null,
LastName nvarchar (50) not null,
EmailAddress nvarchar (50) not null,
PhoneNumber nvarchar (10) not null,
DateOfBirth date ,
DateOfRegistration date,
Gender nvarchar(10),
Password nvarchar(50) not null,
Avatar int ,
nationality nvarchar(10),
Rating int,
PreferredLanguage nvarchar (10)

)

Create Table Helperland.Addresses
(
AddressID int identity (1,1) primary key,
UserID int foreign key references Helperland.Users (userID),
StreetName nvarchar (50),
HomeName nvarchar (50),
PostalCode nvarchar (6),
City nvarchar (10),
PhoneNumber nvarchar(10),

)

Create Table Helperland.Services
(
ServiceID int identity (1,1) primary key,
CustomerID int not null foreign key references Helperland.Users (UserID),
ServiceProviderID int foreign key references Helperland.Users (UserID), 
Date date,
Time time,
InsideCabinate nvarchar (1),
InsideFridge nvarchar (1),
InsideOven nvarchar (1),
laundry nvarchar (1),
InteriorWindow nvarchar (1),
Comments nvarchar (50),
Duration int ,
AddressID int foreign key references Helperland.Addresses (AddressID),
Payment int ,
Status nvarchar (10),
Rating int,
PaymentStatus nvarchar (10),

)

Create Table Helperland.GetOurNewsLetter
(
EmailAddress nvarchar (50) primary key
)