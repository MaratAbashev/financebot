#!/bin/sh

echo "Waiting for Kafka Connect..."

until curl -s http://connect:8083/connectors > /dev/null 2>&1; do
    sleep 5
done

echo "Kafka Connect is ready!"

echo "Creating connector..."

curl -X POST http://connect:8083/connectors \
  -H "Content-Type: application/json" \
  -d '{
    "name": "postgres-connector",
    "config": {
      "connector.class": "io.debezium.connector.postgresql.PostgresConnector",
      "topic.prefix": "postgres",
      "database.hostname": "postgres",
      "database.port": "5432",
      "database.user": "debezium",
      "database.password": "debezium_pass",
      "database.dbname": "finbot_db",
      "plugin.name": "pgoutput",
      "publication.name": "dbz_publication",
      "slot.name": "debezium_slot",
      "table.exclude.list": "public.__EFMigrationsHistory",
      "schema.include.list": "public",
      "snapshot.mode": "initial",
      "topic.creation.enable": true,
      "topic.creation.default.replication.factor": 2,
      "topic.creation.default.partitions": 2
    }
  }'