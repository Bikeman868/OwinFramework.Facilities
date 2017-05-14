/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;

DROP DATABASE IF EXISTS `token`;
CREATE DATABASE IF NOT EXISTS `token` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci */;
USE `token`;

CREATE TABLE IF NOT EXISTS `tbl_token`
(
  `token_id` BIGINT(20) NOT NULL AUTO_INCREMENT,
  `updated` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `token` VARCHAR(30) COLLATE utf8mb4_unicode_ci NOT NULL,
  `type` VARCHAR(30) COLLATE utf8mb4_unicode_ci NOT NULL,
  `identity` VARCHAR(50) COLLATE utf8mb4_unicode_ci NULL,
  `purposes` VARCHAR(2000) COLLATE utf8mb4_unicode_ci NULL,
  `state` VARCHAR(200) COLLATE utf8mb4_unicode_ci NULL,
  PRIMARY KEY (`token_id`),
  KEY `ix_token` (`token`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

/********************************************************************/

DELIMITER //
CREATE PROCEDURE `sp_GetTokenById`
(
	IN `token_id` BIGINT UNSIGNED
) DETERMINISTIC
BEGIN
	SELECT
		t.`token_id`,
		t.`token`,
		t.`type`,
		t.`identity`,
		t.`purposes`,
		t.`state`
	FROM
		`tbl_token` t
	WHERE
		t.`token_id` = `token_id`;
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_GetToken`
(
	IN `token` VARCHAR(30)
) DETERMINISTIC
BEGIN
	SELECT
		t.`token_id`,
		t.`token`,
		t.`type`,
		t.`identity`,
		t.`purposes`,
		t.`state`
	FROM
		`tbl_token` t
	WHERE
		t.`token` = `token`;
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_AddToken`
(
	IN `token` VARCHAR(30),
	IN `type` VARCHAR(30),
	IN `identity` VARCHAR(50),
	IN `purposes` VARCHAR(2000),
	IN `state` VARCHAR(200)
) DETERMINISTIC
BEGIN
	DECLARE token_id BIGINT UNSIGNED;
	
	INSERT INTO tbl_token
	(
		`token`,
		`type`,
		`identity`,
		`purposes`,
		`state`
	)VALUES(
		`token`,
		`type`,
		`identity`,
		`purposes`,
		`state`
	);
	
	SELECT LAST_INSERT_ID() INTO `token_id`;
	
	CALL `sp_GetTokenById`(token_id);
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_UpdateTokenState`
(
	IN `token_id` BIGINT UNSIGNED,
	IN `state` VARCHAR(200)
) DETERMINISTIC
BEGIN
	UPDATE 
		`tbl_token` t
	SET
		t.`state` = `state`
	WHERE
		t.`token_id` = `token_id`;
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_DeleteToken`
(
	IN `token_id` BIGINT UNSIGNED,
	IN `token` VARCHAR(30)
) DETERMINISTIC
BEGIN
	DELETE 
		t
	FROM
		`tbl_token` t
	WHERE
		(t.`token_id` = `token_id` OR `token_id` IS NULL)
			AND
		(t.`token` = `token` OR `token` IS NULL)
			AND
		(`token_id` IS NOT NULL OR `token` IS NOT NULL);
END//
DELIMITER ;

DELIMITER //
CREATE PROCEDURE `sp_clean`()
BEGIN
	DECLARE cutoff DATETIME;
	SET cutoff = DATE_SUB(CURRENT_DATE(), INTERVAL 90 DAY);
	
	DELETE 
		t
	FROM
		`tbl_token` t
	WHERE
		t.`updated` < cutoff;
END//
DELIMITER ;

