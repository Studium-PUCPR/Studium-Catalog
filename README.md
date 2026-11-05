Studium-Catalog (.NET Microservice)

Scope: RF5, RF7, RF8, RF21.

Responsibilities:

Rooms & Blocks: CRUD operations for rooms, capacities, and active/inactive statuses (UC005 / W02–W03).

Interactive Campus Map: Stores floor plan coordinates and reference points (stairs, elevators, restrooms) for room pins (UC021 / M14).

Operating Hours & Maintenance Blocks: Manages weekly opening/closing grids per block or room (UC007 / W04) and temporary maintenance windows (UC008 / W05).

SNS Publisher: Publishes RoomDeactivated, RoomBlocked, and OperatingHoursChanged events to its SNS topic so Studium-Booking can cancel affected reservations.
