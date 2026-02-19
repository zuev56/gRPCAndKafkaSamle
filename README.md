- RocketCoordGenerator генерирует данные и передаёт в Kafka
- FlightController берёт данные из Kafka, анализирует их
- FlightControlPanel берёт результаты анализа данных из FlightController (gRPC), запускает/останавливает RocketCoordGenerator и FlightController (REST)

Проект писался на коленке, чтобы освоить работу с Kafka и gRPC. Поэтому на архитектуру, проверки, DI, и т.п. время не выделял
