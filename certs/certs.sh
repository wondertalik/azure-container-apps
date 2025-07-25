#!/bin/bash

PARENT="dev4"

# Array of DNS entries
DNS_ENTRIES=(
    "localhost"
    "httpapi"
)

# Generate the DNS entries with proper numbering
DNS_SECTION=""
ORDER=1
for DNS in "${DNS_ENTRIES[@]}"; do
    DNS_SECTION+="DNS.${ORDER} = ${DNS}\n"
    ((ORDER++))
    DNS_SECTION+="DNS.${ORDER} = www.${DNS}\n"
    ((ORDER++))
done

openssl req \
-x509 \
-newkey rsa:4096 \
-sha256 \
-days 365 \
-nodes \
-keyout $PARENT.key \
-out $PARENT.crt \
-subj "/CN=${PARENT}" \
-extensions v3_ca \
-extensions v3_req \
-config <( \
  echo '[req]'; \
  echo 'default_bits= 4096'; \
  echo 'distinguished_name=req'; \
  echo 'x509_extension = v3_ca'; \
  echo 'req_extensions = v3_req'; \
  echo '[v3_req]'; \
  echo 'basicConstraints = CA:FALSE'; \
  echo 'keyUsage = nonRepudiation, digitalSignature, keyEncipherment'; \
  echo 'subjectAltName = @alt_names'; \
  echo '[ alt_names ]'; \
  echo -e "${DNS_SECTION}"; \
  echo '[v3_ca]'; \
  echo 'subjectKeyIdentifier=hash'; \
  echo 'authorityKeyIdentifier=keyid:always,issuer'; \
  echo 'basicConstraints = critical, CA:TRUE, pathlen:0'; \
  echo 'keyUsage = critical, cRLSign, keyCertSign'; \
  echo 'extendedKeyUsage = serverAuth, clientAuth')

openssl x509 -noout -text -in $PARENT.crt
openssl x509 -in $PARENT.crt -out $PARENT.pem -outform PEM