#!/bin/bash
# Script ejecutado por LocalStack al iniciar (ready.d).
# Crea la cola SQS que la API va a usar.

echo "Creando cola SQS: import-queue..."
awslocal sqs create-queue --queue-name import-queue
echo "Cola creada."
