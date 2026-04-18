
CREATE DATABASE QLTIENTRINH;
GO 
USE QLTIENTRINH;
GO

--Tạo bảng Resource
CREATE TABLE Resource (
    ResourceId INT PRIMARY KEY IDENTITY(1,1),
    ResourceName NVARCHAR(100) NOT NULL,
    IsShareable BIT DEFAULT 0,       -- có chia sẻ hay không
    HierarchyOrder INT DEFAULT 0,    -- dùng cho circular wait
    Total INT NOT NULL DEFAULT 1     -- quan trọng cho Banker
);

-- Tạo bảng Process
CREATE TABLE Process (
    ProcessId INT PRIMARY KEY IDENTITY(1,1),
    ProcessName NVARCHAR(100) NOT NULL,
    Status NVARCHAR(50) DEFAULT 'Ready',

    HoldingResourceId INT NULL,
    WaitingResourceId INT NULL,

    CONSTRAINT FK_Holding FOREIGN KEY (HoldingResourceId) REFERENCES Resource(ResourceId),
    CONSTRAINT FK_Waiting FOREIGN KEY (WaitingResourceId) REFERENCES Resource(ResourceId)
);
--Bảng trung gian
CREATE TABLE ProcessResource (
    ProcessId INT,
    ResourceId INT,

    Allocation INT DEFAULT 0, -- đang giữ
    Max INT DEFAULT 0,        -- tối đa cần 

    PRIMARY KEY (ProcessId, ResourceId),
    FOREIGN KEY (ProcessId) REFERENCES Process(ProcessId),
    FOREIGN KEY (ResourceId) REFERENCES Resource(ResourceId)
);
-- 3. Chèn dữ liệu mẫu
INSERT INTO Resource (ResourceName, IsShareable, HierarchyOrder) VALUES
(N'R1 - Máy in', 0, 1),
(N'R2 - Máy quét', 0, 2),
(N'R3 - File ReadOnly', 0, 3),
(N'R4 - RAM', 0, 4),
(N'R5 - CPU', 1, 5);

INSERT INTO Process (ProcessName, Status, HoldingResourceId, WaitingResourceId) VALUES

(N'P1 - Chrome.exe', 'Waiting', 1, 2),
(N'P2 - Word.exe', 'Waiting', 2, 3),
(N'P3 - SQLServer.exe', 'Waiting', 3, 4),
(N'P4 - VisualStudio.exe', 'Waiting', 4, 1),
(N'P5 - BackupService', 'Waiting', 5, 1);

INSERT INTO ProcessResource VALUES
(1, 1, 1, 1),
(1, 2, 0, 1),
(2, 1, 0, 1),
(2, 2, 1, 1);

select*from Resource
select *from Process
select*from ProcessResource

