using Microsoft.EntityFrameworkCore.Migrations;

namespace BusTicketingSystem.Migrations
{
    public partial class CreateStoredProcedureForSeats : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create stored procedure to generate seats
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE [dbo].[sp_GenerateSeatsForSchedule]
                    @ScheduleId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    DECLARE @TotalSeats INT;
                    DECLARE @SeatNumber INT;
                    DECLARE @Row NVARCHAR(1);
                    DECLARE @Col INT;
                    DECLARE @SeatChar NVARCHAR(10);
                    
                    -- Get total seats from schedule
                    SELECT @TotalSeats = [TotalSeats]
                    FROM [Schedules]
                    WHERE [ScheduleId] = @ScheduleId;
                    
                    IF @TotalSeats IS NULL OR @TotalSeats = 0
                    BEGIN
                        RAISERROR('Schedule not found or has no seats', 16, 1);
                        RETURN;
                    END
                    
                    SET @SeatNumber = 1;
                    SET @Row = 'A';
                    SET @Col = 1;
                    
                    -- Generate seats based on total seats (max 40)
                    WHILE @SeatNumber <= @TotalSeats AND @SeatNumber <= 40
                    BEGIN
                        SET @SeatChar = @Row + CAST(@Col AS NVARCHAR(2));
                        
                        INSERT INTO [Seats] (
                            [ScheduleId],
                            [SeatNumber],
                            [SeatStatus],
                            [CreatedAt],
                            [IsDeleted]
                        )
                        VALUES (
                            @ScheduleId,
                            @SeatChar,
                            'Available',
                            GETUTCDATE(),
                            0
                        );
                        
                        SET @SeatNumber = @SeatNumber + 1;
                        SET @Col = @Col + 1;
                        
                        -- Move to next row after 4 columns
                        IF @Col > 4
                        BEGIN
                            SET @Col = 1;
                            SET @Row = CHAR(ASCII(@Row) + 1);
                        END
                    END
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[sp_GenerateSeatsForSchedule]");
        }
    }
}
