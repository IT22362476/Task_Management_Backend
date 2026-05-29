# =============================================================================
# Random resources (passwords and unique name suffixes)
# =============================================================================

# Random suffix for ACR name (globally unique)
resource "random_id" "acr_suffix" {
  byte_length = 4
}

resource "random_password" "postgres_admin" {
  length           = 24
  special          = true
  override_special = "!#$%&()*+,-.:;<=>?@[]^_{|}~"
  min_upper        = 2
  min_lower        = 2
  min_numeric      = 2
  min_special      = 2
}

resource "random_password" "postgres_app" {
  length           = 24
  special          = true
  override_special = "!#$%&()*+,-.:;<=>?@[]^_{|}~"
  min_upper        = 2
  min_lower        = 2
  min_numeric      = 2
  min_special      = 2
}

# JWT signing key (long, alphanumeric – no special chars to avoid config issues)
resource "random_password" "jwt_key" {
  length  = 64
  special = false
  min_upper = 2
  min_lower = 2
  min_numeric = 2
}
