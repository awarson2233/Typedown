-- Schema found in databases created by earlier Typedown releases (before the EF migration existed): string
-- columns are NOT NULL, and the migration history was bootstrapped later with ProductVersion 3.1.30.
CREATE TABLE "ExportConfig" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ExportConfig" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Notes" TEXT NOT NULL,
    "Type" INTEGER NOT NULL,
    "Config" TEXT NOT NULL
);

CREATE TABLE "FileAccessHistory" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_FileAccessHistory" PRIMARY KEY AUTOINCREMENT,
    "AccessTime" TEXT NOT NULL,
    "FilePath" TEXT NOT NULL
);

CREATE TABLE "FolderAccessHistory" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_FolderAccessHistory" PRIMARY KEY AUTOINCREMENT,
    "AccessTime" TEXT NOT NULL,
    "FolderPath" TEXT NOT NULL
);

CREATE TABLE "ImageUploadConfig" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ImageUploadConfig" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Notes" TEXT NOT NULL,
    "IsEnable" INTEGER NOT NULL,
    "Method" INTEGER NOT NULL,
    "Config" TEXT NOT NULL
);
