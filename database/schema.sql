-- Reference schema generated from the actual demo database.
PRAGMA foreign_keys = ON;

CREATE TABLE "Books" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Books" PRIMARY KEY AUTOINCREMENT,
    "Title" TEXT NOT NULL,
    "Author" TEXT NOT NULL,
    "Category" TEXT NOT NULL,
    "Publisher" TEXT NOT NULL,
    "Isbn" TEXT NOT NULL,
    "Year" INTEGER NOT NULL,
    "Quantity" INTEGER NOT NULL,
    "Shelf" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "Color" TEXT NOT NULL,
    CONSTRAINT "CK_Book_Quantity" CHECK (Quantity >= 0)
);

CREATE TABLE "Loans" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Loans" PRIMARY KEY AUTOINCREMENT,
    "BookId" INTEGER NOT NULL,
    "MemberId" INTEGER NOT NULL,
    "BorrowedAt" TEXT NOT NULL,
    "DueAt" TEXT NOT NULL,
    "ReturnedAt" TEXT NULL,
    "Renewals" INTEGER NOT NULL,
    "Fine" INTEGER NOT NULL,
    "FinePaid" INTEGER NOT NULL,
    CONSTRAINT "FK_Loans_Books_BookId" FOREIGN KEY ("BookId") REFERENCES "Books" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Loans_Members_MemberId" FOREIGN KEY ("MemberId") REFERENCES "Members" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Members" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Members" PRIMARY KEY AUTOINCREMENT,
    "FullName" TEXT NOT NULL,
    "Email" TEXT NOT NULL,
    "Phone" TEXT NULL,
    "Type" TEXT NOT NULL,
    "ExpiresAt" TEXT NOT NULL,
    "Active" INTEGER NOT NULL
);

CREATE TABLE "Reservations" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Reservations" PRIMARY KEY AUTOINCREMENT,
    "BookId" INTEGER NOT NULL,
    "MemberId" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    CONSTRAINT "FK_Reservations_Books_BookId" FOREIGN KEY ("BookId") REFERENCES "Books" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Reservations_Members_MemberId" FOREIGN KEY ("MemberId") REFERENCES "Members" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Users" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY AUTOINCREMENT,
    "Username" TEXT NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "FullName" TEXT NOT NULL,
    "Role" TEXT NOT NULL,
    "Active" INTEGER NOT NULL,
    "MemberId" INTEGER NULL,
    CONSTRAINT "FK_Users_Members_MemberId" FOREIGN KEY ("MemberId") REFERENCES "Members" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_Loans_BookId" ON "Loans" ("BookId");

CREATE INDEX "IX_Loans_MemberId" ON "Loans" ("MemberId");

CREATE UNIQUE INDEX "IX_Members_Email" ON "Members" ("Email");

CREATE INDEX "IX_Reservations_BookId" ON "Reservations" ("BookId");

CREATE INDEX "IX_Reservations_MemberId" ON "Reservations" ("MemberId");

CREATE UNIQUE INDEX "IX_Users_MemberId" ON "Users" ("MemberId");

CREATE UNIQUE INDEX "IX_Users_Username" ON "Users" ("Username");
