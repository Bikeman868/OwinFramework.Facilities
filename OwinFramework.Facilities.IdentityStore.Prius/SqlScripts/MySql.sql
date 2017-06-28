/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;

DROP DATABASE IF EXISTS `identity`;
CREATE DATABASE IF NOT EXISTS `identity` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci */;
USE `identity`;

CREATE TABLE IF NOT EXISTS `tbl_claim`
(
  `claim_id` BIGINT(20) NOT NULL AUTO_INCREMENT,
  `identity_id` BIGINT(20) unsigned NOT NULL,
  `name` VARCHAR(30) COLLATE utf8mb4_unicode_ci NOT NULL,
  `value` VARCHAR(200) COLLATE utf8mb4_unicode_ci NOT NULL,
  `status` INT NOT NULL,
  PRIMARY KEY (`claim_id`),
  KEY `ix_identity` (`identity_id`),
  UNIQUE INDEX `ix_name` (`identity_id`, `name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `tbl_credential`
(
  `credential_id` BIGINT(20) unsigned NOT NULL AUTO_INCREMENT,
  `identity_id` BIGINT(20) unsigned NOT NULL,
  `username` VARCHAR(80) COLLATE utf8mb4_unicode_ci NOT NULL,
  `purposes` VARCHAR(2048) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `version` INT(11) NOT NULL,
  `hash` VARBINARY(32) NOT NULL,
  `salt` VARBINARY(16) NOT NULL,
  `fail_count` INT unsigned DEFAULT 0,
  `locked` DATETIME DEFAULT NULL,
  PRIMARY KEY (`credential_id`),
  KEY `ix_identity` (`identity_id`),
  UNIQUE INDEX `ix_username` (`username`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `tbl_identity`
(
  `identity_id` BIGINT(20) NOT NULL AUTO_INCREMENT,
  `identity` VARCHAR(50) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '0',
  PRIMARY KEY (`identity_id`),
  UNIQUE INDEX `ix_identity` (`identity`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `tbl_secret`
(
  `secret_id` BIGINT(20) NOT NULL AUTO_INCREMENT,
  `identity_id` BIGINT(20) NOT NULL,
  `name` VARCHAR(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `secret` VARCHAR(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `purposes` VARCHAR(2048) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`secret_id`),
  KEY `ix_identity` (`identity_id`),
  UNIQUE INDEX `ix_secret` (`secret`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `tbl_authenticate`
(
  `authenticate_id` BIGINT(20) NOT NULL AUTO_INCREMENT,
  `when` DATETIME NOT NULL,
  `identity_id` BIGINT(20) NOT NULL,
  `purposes` VARCHAR(2048) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `remember_me_token` VARCHAR(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `authenticate_method` VARCHAR(20) COLLATE utf8mb4_unicode_ci NOT NULL,
  `method_id` BIGINT(20) NOT NULL,
  `expires` DATETIME DEFAULT NULL,
  PRIMARY KEY (`authenticate_id`),
  KEY `ix_identity` (`identity_id`),
  UNIQUE INDEX `ix_remember_me_token` (`remember_me_token`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `tbl_audit`
(
  `audit_id` BIGINT(20) NOT NULL AUTO_INCREMENT,
  `when` DATETIME NOT NULL,
  `who_id` BIGINT(20) DEFAULT NULL,
  `reason` VARCHAR(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `identity_id` BIGINT(20) NOT NULL,
  `action` VARCHAR(30) COLLATE utf8mb4_unicode_ci NOT NULL,
  `original_value` VARCHAR(120) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `new_value` VARCHAR(120) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`audit_id`),
  KEY `ix_identity` (`identity_id`),
  KEY `ix_who` (`who_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

/********************************************************************/

DELIMITER //
CREATE PROCEDURE `sp_AddIdentity`
(
	IN `who_identity` VARCHAR(50),
	IN `reason` VARCHAR(50),
	IN `identity` VARCHAR(50)
) DETERMINISTIC
BEGIN
	DECLARE identity_id BIGINT UNSIGNED;
	DECLARE who_id BIGINT UNSIGNED;
	
	SELECT
		i.identity_id
	INTO
		who_id
	FROM
		tbl_identity i
	WHERE
		i.identity = who_identity;
	
	INSERT INTO tbl_identity
	(
		identity
	)VALUES(
		identity
	);
	
	SELECT LAST_INSERT_ID() INTO `identity_id`;
	
	INSERT INTO tbl_audit
	(
		`when`,
		`who_id`,
		`reason`,
		`identity_id`,
		`action`,
		`original_value`,
		`new_value`
	) VALUES (
		UTC_TIMESTAMP(),
		`who_id`,
		`reason`,
		`identity_id`,
		'Add identity',
		NULL,
		`identity`
	);

	SELECT
		i.identity_id,
		i.identity
	FROM
		tbl_identity i
	WHERE
		i.identity_id = `identity_id`;
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_AddClaim`
(
	IN `who_identity` VARCHAR(50),
	IN `reason` VARCHAR(50),
	IN `identity` VARCHAR(50), 
	IN `name` VARCHAR(30), 
	IN `value` VARCHAR(200), 
	IN `status` INT
) DETERMINISTIC
BEGIN
	DECLARE identity_id BIGINT UNSIGNED;
	DECLARE who_id BIGINT UNSIGNED;
	
	SELECT
		i.identity_id
	INTO
		who_id
	FROM
		tbl_identity i
	WHERE
		i.identity = who_identity;
	
	SELECT
		i.identity_id
	INTO
		identity_id
	FROM
		tbl_identity i
	WHERE
		i.identity = identity;
		
	INSERT INTO tbl_audit
	(
		`when`,
		`who_id`,
		`reason`,
		`identity_id`,
		`action`,
		`original_value`,
		`new_value`
	) VALUES (
		UTC_TIMESTAMP(),
		`who_id`,
		`reason`,
		`identity_id`,
		'Add claim',
		NULL,
		CONCAT(`name`, '=', `value`)
	);
	
	INSERT INTO tbl_claim
	(
		`identity_id`,
		`name`,
		`value`,
		`status`
	)VALUES(
		`identity_id`,
		`name`,
		`value`,
		`status`
	);
	
	SELECT
		c.`claim_id`,
		`identity`,
		c.`identity_id`,
		c.`name`,
		c.`value`,
		c.`status`
	FROM
		tbl_claim c
	WHERE
		c.`claim_id` = LAST_INSERT_ID();
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_AddCredential`
(
	IN `who_identity` VARCHAR(50),
	IN `reason` VARCHAR(50),
	IN `identity` VARCHAR(50), 
	IN `username` VARCHAR(80), 
	IN `purposes` VARCHAR(2048), 
	IN `version` INT, 
	IN `hash` VARBINARY(32), 
	IN `salt` VARBINARY(16)
) DETERMINISTIC
BEGIN
	DECLARE identity_id BIGINT UNSIGNED;
	DECLARE who_id BIGINT UNSIGNED;
	
	SELECT
		i.identity_id
	INTO
		who_id
	FROM
		tbl_identity i
	WHERE
		i.identity = who_identity;
	
	SELECT
		i.identity_id
	INTO
		identity_id
	FROM
		tbl_identity i
	WHERE
		i.identity = identity;
		
	INSERT INTO tbl_audit
	(
		`when`,
		`who_id`,
		`reason`,
		`identity_id`,
		`action`,
		`original_value`,
		`new_value`
	) VALUES (
		UTC_TIMESTAMP(),
		`who_id`,
		`reason`,
		`identity_id`,
		'Add credential',
		NULL,
		`username`
	);
	
	INSERT INTO tbl_credential
	(
		`identity_id`,
		`username`,
		`purposes`,
		`version`,
		`hash`,
		`salt`
	)VALUES(
		`identity_id`,
		`username`,
		`purposes`,
		`version`,
		`hash`,
		`salt`
	);
	
	SELECT
		c.`credential_id`,
		`identity`,
		c.`identity_id`,
		c.`username`,
		c.`purposes`,
		c.`version`,
		c.`hash`,
		c.`salt`
	FROM
		tbl_credential c
	WHERE
		c.`credential_id` = LAST_INSERT_ID();
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_AddSharedSecret`
(
	IN `who_identity` VARCHAR(50),
	IN `reason` VARCHAR(50),
	IN `identity` VARCHAR(50), 
	IN `name` VARCHAR(100), 
	IN `secret` VARCHAR(50),
	IN `purposes` VARCHAR(2048)
) DETERMINISTIC
BEGIN	
	DECLARE identity_id BIGINT UNSIGNED;
	DECLARE who_id BIGINT UNSIGNED;
	
	SELECT
		i.identity_id
	INTO
		who_id
	FROM
		tbl_identity i
	WHERE
		i.identity = who_identity;
	
	SELECT
		i.identity_id
	INTO
		identity_id
	FROM
		tbl_identity i
	WHERE
		i.identity = identity;
		
	INSERT INTO tbl_audit
	(
		`when`,
		`who_id`,
		`reason`,
		`identity_id`,
		`action`,
		`original_value`,
		`new_value`
	) VALUES (
		UTC_TIMESTAMP(),
		`who_id`,
		`reason`,
		`identity_id`,
		'Add secret',
		NULL,
		CONCAT(`identity`, ' ', `name`)
	);

	INSERT INTO tbl_secret
	(
		`identity_id`,
		`name`,
		`secret`,
		`purposes`
	)VALUES(
		`identity_id`,
		`name`,
		`secret`,
		`purposes`
	);
	
	SELECT
		s.`secret_id`
		`identity`,
		s.`identity_id`,
		s.`name`,
		s.`secret`,
		s.`purposes`
	FROM
		tbl_secret s
	WHERE
		s.secret_id = LAST_INSERT_ID();
END//
DELIMITER ;

/********************************************************************/

DELIMITER //
CREATE PROCEDURE `sp_AuthenticateSuccess`
(
	IN `identity` VARCHAR(50), 
	IN `purposes` VARCHAR(2048),
	IN `remember_me_token` VARCHAR(50),
	IN `authenticate_method` VARCHAR(20),
	IN `method_id` BIGINT UNSIGNED,
	IN `expires` DATETIME
) DETERMINISTIC
BEGIN	
	DECLARE identity_id BIGINT UNSIGNED;
	
	SELECT
		i.identity_id
	INTO
		identity_id
	FROM
		tbl_identity i
	WHERE
		i.identity = identity;
		
	INSERT INTO tbl_authenticate
	(
		`when`,
		`identity_id`,
		`purposes`,
		`remember_me_token`,
		`authenticate_method`,
		`method_id`,
		`expires`
	) VALUES (
		UTC_TIMESTAMP(),
		`identity_id`,
		`purposes`,
		`remember_me_token`,
		`authenticate_method`,
		`method_id`,
		`expires`
	);
	
	IF `authenticate_method` = 'Credentials' THEN
		UPDATE
			tbl_credential c
		SET
			c.fail_count = 0,
			c.locked = NULL
		WHERE
			c.credential_id = `method_id`;
	END IF;
	
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_AuthenticateFail`
(
	IN `identity` VARCHAR(50), 
	IN `authenticate_method` VARCHAR(20),
	IN `method_id` BIGINT UNSIGNED,
	OUT `fail_count` INT UNSIGNED
) DETERMINISTIC
BEGIN	
	DECLARE identity_id BIGINT UNSIGNED;
	
	SELECT
		i.identity_id
	INTO
		identity_id
	FROM
		tbl_identity i
	WHERE
		i.identity = identity;
		
	IF `authenticate_method` = 'Credentials' THEN
		SELECT
			c.fail_count
		INTO
			`fail_count`
		FROM
			tbl_credential c
		WHERE
			c.credential_id = `method_id`;

		IF fail_count IS NULL THEN SET fail_count = 1;
		ELSE SET fail_count = fail_count + 1;
		END IF;
		
		UPDATE
			tbl_credential c
		SET
			c.fail_count = `fail_count`
		WHERE
			c.credential_id = `method_id`;
	END IF;
	
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_LockUsername`
(
	IN `username` VARCHAR(80)
) DETERMINISTIC
BEGIN	
	DECLARE identity_id BIGINT UNSIGNED;

	UPDATE
		tbl_credential c
	SET
		c.locked = UTC_TIMESTAMP()
	WHERE
		c.username = `username`;

	SELECT
		c.identity_id
	INTO
		identity_id
	FROM
		tbl_credential c
	WHERE
		c.username = `username`;

	INSERT INTO tbl_audit
	(
		`when`,
		`who_id`,
		`reason`,
		`identity_id`,
		`action`,
		`original_value`,
		`new_value`
	) VALUES (
		UTC_TIMESTAMP(),
		NULL,
		'Authentication failed',
		`identity_id`,
		'Account locked',
		NULL,
		NULL
	);
	
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_UnlockUsername`
(
	IN `username` VARCHAR(80)
) DETERMINISTIC
BEGIN	
	DECLARE identity_id BIGINT UNSIGNED;

	UPDATE
		tbl_credential c
	SET
		c.locked = NULL,
		c.fail_count = 0
	WHERE
		c.username = `username`;
		
	SELECT
		c.identity_id
	INTO
		identity_id
	FROM
		tbl_credential c
	WHERE
		c.username = `username`;

	INSERT INTO tbl_audit
	(
		`when`,
		`who_id`,
		`reason`,
		`identity_id`,
		`action`,
		`original_value`,
		`new_value`
	) VALUES (
		UTC_TIMESTAMP(),
		NULL,
		'Lock expired',
		`identity_id`,
		'Account unlocked',
		NULL,
		NULL
	);
END//
DELIMITER ;

/********************************************************************/

DELIMITER //
CREATE PROCEDURE `sp_DeleteClaim`
(
	IN `who_identity` VARCHAR(50),
	IN `reason` VARCHAR(50),
	IN `claim_id` BIGINT UNSIGNED
) DETERMINISTIC
BEGIN
	DECLARE who_id BIGINT UNSIGNED;
	DECLARE identity_id BIGINT UNSIGNED;
	
	SELECT
		i.identity_id
	INTO
		who_id
	FROM
		tbl_identity i
	WHERE
		i.identity = who_identity;
	
	SELECT
		c.identity_id
	INTO
		identity_id
	FROM
		tbl_claim c
	WHERE
		c.claim_id = `claim_id`;
	
	INSERT INTO tbl_audit
	(
		`when`,
		`who_id`,
		`reason`,
		`identity_id`,
		`action`,
		`original_value`,
		`new_value`
	) VALUES (
		UTC_TIMESTAMP(),
		`who_id`,
		`reason`,
		`identity_id`,
		'Delete claim',
		`claim_id`,
		NULL
	);

	DELETE 
	FROM 
		c USING tbl_claim AS c
	WHERE 
		c.claim_id = claim_id;
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_DeleteCredential`
(
	IN `who_identity` VARCHAR(50),
	IN `reason` VARCHAR(50),
	IN `credential_id` BIGINT UNSIGNED
) DETERMINISTIC
BEGIN
	DECLARE who_id BIGINT UNSIGNED;
	DECLARE identity_id BIGINT UNSIGNED;
	
	SELECT
		i.identity_id
	INTO
		who_id
	FROM
		tbl_identity i
	WHERE
		i.identity = who_identity;
	
	SELECT
		c.identity_id
	INTO
		identity_id
	FROM
		tbl_credential c
	WHERE
		c.credential_id = `credential_id`;
	
	INSERT INTO tbl_audit
	(
		`when`,
		`who_id`,
		`reason`,
		`identity_id`,
		`action`,
		`original_value`,
		`new_value`
	) VALUES (
		UTC_TIMESTAMP(),
		`who_id`,
		`reason`,
		`identity_id`,
		'Delete credential',
		`credential_id`,
		NULL
	);

	DELETE 
	FROM 
		c USING tbl_credential AS c
	WHERE 
		c.credential_id = credential_id;
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_DeleteIdentityCredentials`
(
	IN `who_identity` VARCHAR(50),
	IN `reason` VARCHAR(50),
	IN `identity` VARCHAR(50)
) DETERMINISTIC
BEGIN
	DECLARE identity_id BIGINT UNSIGNED;
	DECLARE who_id BIGINT UNSIGNED;
	
	SELECT
		i.identity_id
	INTO
		who_id
	FROM
		tbl_identity i
	WHERE
		i.identity = who_identity;
	
	SELECT
		i.identity_id
	INTO
		identity_id
	FROM
		tbl_identity i
	WHERE
		i.identity = identity;
		
	INSERT INTO tbl_audit
	(
		`when`,
		`who_id`,
		`reason`,
		`identity_id`,
		`action`,
		`original_value`,
		`new_value`
	) VALUES (
		UTC_TIMESTAMP(),
		`who_id`,
		`reason`,
		`identity_id`,
		'Delete credentials',
		NULL,
		NULL
	);

	DELETE 
	FROM 
		c USING tbl_credential AS c
	WHERE 
		c.identity_id = identity_id;
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_DeleteUsernameCredentials`
(
	IN `who_identity` VARCHAR(50),
	IN `reason` VARCHAR(50),
	IN `username` VARCHAR(80)
) DETERMINISTIC
BEGIN
	DECLARE identity_id BIGINT UNSIGNED;
	DECLARE who_id BIGINT UNSIGNED;
	
	SELECT
		i.identity_id
	INTO
		who_id
	FROM
		tbl_identity i
	WHERE
		i.identity = who_identity;
	
	SELECT
		c.identity_id
	INTO
		identity_id
	FROM
		tbl_credential c
	WHERE
		c.username = username;

	INSERT INTO tbl_audit
	(
		`when`,
		`who_id`,
		`reason`,
		`identity_id`,
		`action`,
		`original_value`,
		`new_value`
	) VALUES (
		UTC_TIMESTAMP(),
		`who_id`,
		`reason`,
		`identity_id`,
		'Delete username',
		`username`,
		NULL
	);

	DELETE 
	FROM 
		c USING tbl_credential AS c
	WHERE 
		c.username = username;
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_DeleteSharedSecret`
(
	IN `who_identity` VARCHAR(50),
	IN `reason` VARCHAR(50),
	IN `secret` VARCHAR(50)
) DETERMINISTIC
BEGIN
	DECLARE identity_id BIGINT UNSIGNED;
	DECLARE who_id BIGINT UNSIGNED;
	
	SELECT
		i.identity_id
	INTO
		who_id
	FROM
		tbl_identity i
	WHERE
		i.identity = who_identity;

	SELECT
		s.identity_id
	INTO
		identity_id
	FROM
		tbl_secret s
	WHERE
		s.secret = secret;

	INSERT INTO tbl_audit
	(
		`when`,
		`who_id`,
		`reason`,
		`identity_id`,
		`action`,
		`original_value`,
		`new_value`
	) VALUES (
		UTC_TIMESTAMP(),
		`who_id`,
		`reason`,
		`identity_id`,
		'Delete secret',
		`secret`,
		NULL
	);

	DELETE 
	FROM 
		s USING tbl_secret AS s
	WHERE 
		s.secret = secret;
END//
DELIMITER ;

/********************************************************************/

DELIMITER //
CREATE PROCEDURE `sp_Purge`
(
	IN `start_date` DATETIME
) DETERMINISTIC
BEGIN
	DELETE 
	FROM 
		a USING tbl_audit AS a
	WHERE 
		a.`when` < start_date;

	DELETE 
	FROM 
		a USING tbl_authenticate AS a
	WHERE 
		a.`when` < start_date;
END//
DELIMITER ;

/********************************************************************/

DELIMITER //
CREATE PROCEDURE `sp_UpdateCredentialPassword`
(
	IN `who_identity` VARCHAR(50),
	IN `reason` VARCHAR(50),
	IN `username` VARCHAR(80), 
	IN `version` INT, 
	IN `hash` VARBINARY(32), 
	IN `salt` VARBINARY(16)
) DETERMINISTIC
BEGIN
	DECLARE identity_id BIGINT UNSIGNED;
	DECLARE who_id BIGINT UNSIGNED;
	
	SELECT
		i.identity_id
	INTO
		who_id
	FROM
		tbl_identity i
	WHERE
		i.identity = who_identity;

	SELECT
		c.identity_id
	INTO
		identity_id
	FROM
		tbl_credential c
	WHERE
		c.username = username;

	INSERT INTO tbl_audit
	(
		`when`,
		`who_id`,
		`reason`,
		`identity_id`,
		`action`,
		`original_value`,
		`new_value`
	) VALUES (
		UTC_TIMESTAMP(),
		`who_id`,
		`reason`,
		`identity_id`,
		'Update credential password',
		CONCAT(`username`, ' <old_password>'),
		CONCAT(`username`, ' <new_password>')
	);
	
	UPDATE
		tbl_credential c
	SET
		c.`version` = `version`,
		c.`hash` = `hash`,
		c.`salt` = `salt`
	WHERE
		c.username = username;
		
	SELECT
		c.`credential_id`,
		c.`identity_id`,
		i.`identity`,
		c.`username`,
		c.`purposes`,
		c.`version`,
		c.`hash`,
		c.`salt`,
		c.`fail_count`,
		c.`locked`
	FROM
		tbl_credential c
			JOIN
		tbl_identity i ON c.identity_id = i.identity_id
	WHERE
		c.username = username;
END//
DELIMITER ;

/********************************************************************/

DELIMITER //
CREATE PROCEDURE `sp_GetIdentity`
(
	IN `identity` VARCHAR(50)
) DETERMINISTIC
BEGIN
	SELECT
		i.identity_id,
		i.identity
	FROM
		tbl_identity i
	WHERE
		i.identity = identity;
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_GetIdentitySharedSecrets`
(
	IN `identity` VARCHAR(50)
) DETERMINISTIC
BEGIN
	DECLARE identity_id BIGINT UNSIGNED;
	
	SELECT
		i.identity_id
	INTO
		identity_id
	FROM
		tbl_identity i
	WHERE
		i.identity = identity;

	SELECT
		s.secret_id,
		s.identity_id,
		s.name,
		s.secret,
		identity
	FROM
		tbl_secret s
	WHERE
		s.identity_id = identity_id;
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_GetSharedSecret`
(
	IN `secret` VARCHAR(50)
) DETERMINISTIC
BEGIN
	SELECT
		s.`secret_id`,
		s.`identity_id`,
		s.`name`,
		s.`secret`,
		i.`identity`,
		s.`purposes`
	FROM
		tbl_secret s
			JOIN
		tbl_identity i ON s.identity_id = i.identity_id
	WHERE
		s.secret = secret;
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_GetUsernameCredential`
(
	IN `username` VARCHAR(80)
) DETERMINISTIC
BEGIN
	SELECT
		c.`credential_id`,
		c.`identity_id`,
		i.`identity`,
		c.`username`,
		c.`purposes`,
		c.`version`,
		c.`hash`,
		c.`salt`,
		c.`fail_count`,
		c.`locked`
	FROM
		tbl_credential c
			JOIN
		tbl_identity i ON c.identity_id = i.identity_id
	WHERE
		c.username = username;
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_GetAudit`
(
	IN `identity` VARCHAR(50)
) DETERMINISTIC
BEGIN
	SELECT
		a.`when`,
		a.who_id,
		i1.identity AS who_identity,
		a.reason,
		a.identity_id,
		i2.identity AS identity,
		a.`action`,
		a.original_value,
		a.new_value
	FROM
		tbl_audit a
			LEFT JOIN
		tbl_identity i1 ON a.who_id = i1.identity_id
			LEFT JOIN
		tbl_identity i2 ON a.identity_id = i2.identity_id
	WHERE
		i1.identity = `identity`
			OR
		i2.identity = `identity`
	ORDER BY
		a.audit_id DESC
	LIMIT 5000;
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_GetIdentityAuthentications`
(
	IN `identity` VARCHAR(50)
) DETERMINISTIC
BEGIN
	SELECT
		a.authenticate_id,
		a.`when`,
		a.identity_id,
		i.identity,
		a.purposes,
		a.remember_me_token,
		a.authenticate_method,
		a.method_id,
		a.expires
	FROM
		tbl_authenticate a
			JOIN
		tbl_identity i ON a.identity_id = i.identity_id
	WHERE
		i.identity = `identity`
	ORDER BY
		a.`when` DESC
	LIMIT 5000;
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_GetAuthenticationToken`
(
	IN `remember_me_token` VARCHAR(50)
) DETERMINISTIC
BEGIN
	SELECT
		a.authenticate_id,
		a.`when`,
		a.identity_id,
		i.identity,
		a.purposes,
		a.remember_me_token,
		a.authenticate_method,
		a.method_id,
		a.expires
	FROM
		tbl_authenticate a
			JOIN
		tbl_identity i ON a.identity_id = i.identity_id
	WHERE
		a.remember_me_token = `remember_me_token`;
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_GetCredential`
(
	IN `credential_id` BIGINT UNSIGNED
) DETERMINISTIC
BEGIN
	SELECT
		c.credential_id,
		c.identity_id,
		i.`identity`,
		c.username,
		c.purposes,
		c.`version`,
		c.`hash`,
		c.`salt`,
		c.fail_count,
		c.locked
	FROM
		tbl_credential c
			JOIN
		tbl_identity i ON c.identity_id = i.identity_id
	WHERE
		c.credential_id = credential_id;
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_GetIdentityCredentials`
(
	IN `identity` VARCHAR(50)
) DETERMINISTIC
BEGIN
	SELECT
		c.credential_id,
		c.identity_id,
		i.`identity`,
		c.username,
		c.purposes,
		c.`version`,
		c.`hash`,
		c.`salt`,
		c.fail_count,
		c.locked
	FROM
		tbl_credential c
			JOIN
		tbl_identity i ON c.identity_id = i.identity_id
	WHERE
		i.identity = `identity`;
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_GetIdentityClaims`
(
	IN `identity` VARCHAR(50)
) DETERMINISTIC
BEGIN
	SELECT
		c.claim_id,
		c.identity_id,
		i.`identity`,
		c.`name`,
		c.`value`,
		c.`status`
	FROM
		tbl_claim c
			JOIN
		tbl_identity i ON c.identity_id = i.identity_id
	WHERE
		i.identity = `identity`;
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_SearchIdentities`
(
	IN `searchText` VARCHAR(50)
) DETERMINISTIC
BEGIN
	SELECT
		i.`identity_id`,
		i.`identity`,
		c.`claim_id`,
		c.`name`,
		c.`value`,
		c.`status`
	FROM
		tbl_identity i
			LEFT JOIN
		tbl_claim c ON i.identity_id = c.identity_id
	WHERE
		i.identity LIKE CONCAT('%', searchText, '%')
			OR
		(
			c.`status` = 1
				AND
			c.value LIKE CONCAT('%', searchText, '%')
		);
END//
DELIMITER ;

/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IF(@OLD_FOREIGN_KEY_CHECKS IS NULL, 1, @OLD_FOREIGN_KEY_CHECKS) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
