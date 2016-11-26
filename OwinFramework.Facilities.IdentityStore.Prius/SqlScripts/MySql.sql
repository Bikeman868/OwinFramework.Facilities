/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;

DROP DATABASE IF EXISTS `identity`;
CREATE DATABASE IF NOT EXISTS `identity` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci */;
USE `identity`;

CREATE TABLE IF NOT EXISTS `tbl_credential`
(
  `credential_id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `identity_id` bigint(20) unsigned NOT NULL,
  `username` varchar(80) COLLATE utf8mb4_unicode_ci NOT NULL,
  `purposes` varchar(2048) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `version` int(11) NOT NULL,
  `hash` varbinary(32) NOT NULL,
  `salt` varbinary(16) NOT NULL,
  PRIMARY KEY (`credential_id`),
  KEY `ix_identity` (`identity_id`),
  UNIQUE INDEX `ix_username` (`username`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `tbl_identity`
(
  `identity_id` bigint(20) NOT NULL AUTO_INCREMENT,
  `identity` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT '0',
  PRIMARY KEY (`identity_id`),
  UNIQUE INDEX `ix_identity` (`identity`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `tbl_secret`
(
  `secret_id` bigint(20) NOT NULL AUTO_INCREMENT,
  `identity_id` bigint(20) NOT NULL,
  `name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `secret` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`secret_id`),
  KEY `ix_identity` (`identity_id`),
  UNIQUE INDEX `ix_secret` (`secret`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

DELIMITER //
CREATE PROCEDURE `sp_AddCredential`
(
	IN `identity` VARCHAR(50), 
	IN `username` VARCHAR(80), 
	IN `purposes` VARCHAR(2048), 
	IN `version` INT, 
	IN `hash` VARBINARY(32), 
	IN `salt` VARBINARY(16)
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
CREATE PROCEDURE `sp_AddIdentity`
(
	IN `identity` VARCHAR(50)
) DETERMINISTIC
BEGIN
	INSERT INTO tbl_identity
	(
		identity
	)VALUES(
		identity
	);
	
	SELECT
		i.identity_id,
		i.identity
	FROM
		tbl_identity i
	WHERE
		i.identity_id = LAST_INSERT_ID();
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_AddSharedSecret`
(
	IN `identity` VARCHAR(50), 
	IN `name` VARCHAR(100), 
	IN `secret` VARCHAR(50)
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
		
	INSERT INTO tbl_secret
	(
		`identity_id`,
		`name`,
		`secret`
	)VALUES(
		`identity_id`,
		`name`,
		`secret`
	);
	
	SELECT
		s.`secret_id`
		`identity`,
		s.`identity_id`,
		s.`name`,
		s.`secret`
	FROM
		tbl_secret s
	WHERE
		s.secret_id = LAST_INSERT_ID();
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_DeleteIdentityCredentials`
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
	IN `username` VARCHAR(80)
) DETERMINISTIC
BEGIN
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
	IN `secret` VARCHAR(50)
) DETERMINISTIC
BEGIN
	DELETE 
	FROM 
		s USING tbl_secret AS s
	WHERE 
		s.secret = secret;
END//
DELIMITER ;

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
		s.secret_id,
		s.identity_id,
		s.name,
		s.secret,
		i.identity
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
		c.`salt`
	FROM
		tbl_credential c
			JOIN
		tbl_identity i ON c.identity_id = i.identity_id
	WHERE
		c.username = username;
END//
DELIMITER ;
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IF(@OLD_FOREIGN_KEY_CHECKS IS NULL, 1, @OLD_FOREIGN_KEY_CHECKS) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
