
CREATE DATABASE QLTIENTRINH;
GO 
USE QLTIENTRINH;
GO

--Tạo bảng Resources
CREATE TABLE Resources (
    ResourceId INT PRIMARY KEY IDENTITY(1,1),
    ResourceName NVARCHAR(100) NOT NULL,
    IsShareable BIT DEFAULT 0, -- 0: Độc quyền, 1: Chia sẻ
    HierarchyOrder INT DEFAULT 0, -- Thứ tự ưu tiên (Dùng cho Circular Wait)
    IsAvailable BIT DEFAULT 1 -- Trạng thái tài nguyên
);

-- Tạo bảng Processes (Đã thêm IDENTITY)
CREATE TABLE Processes (
    ProcessId INT PRIMARY KEY IDENTITY(1,1), -- Thêm IDENTITY ở đây
    ProcessName NVARCHAR(100) NOT NULL,
    Status NVARCHAR(50) DEFAULT 'Ready', -- Ready, Running, Waiting...
    HoldingResourceId INT NULL,
    WaitingResourceId INT NULL,
    CONSTRAINT FK_Holding FOREIGN KEY (HoldingResourceId) REFERENCES Resources(ResourceId),
    CONSTRAINT FK_Waiting FOREIGN KEY (WaitingResourceId) REFERENCES Resources(ResourceId)
);

-- 3. Chèn dữ liệu mẫu
INSERT INTO Resources (ResourceName, IsShareable, HierarchyOrder, IsAvailable) VALUES 
(N'Máy in (R1)', 0, 1, 1),
(N'Máy quét (R2)', 0, 2, 1),
(N'File Read-Only (R3)', 1, 3, 1);

INSERT INTO Processes (ProcessName, Status, HoldingResourceId, WaitingResourceId) VALUES 
(N'Tiến trình P1', 'Waiting', 1, 2), 
(N'Tiến trình P2', 'Waiting', 2, 1); -- P2 giữ R2 đợi R1 (Đây là vòng lặp gây Deadlock)

select*from Resources