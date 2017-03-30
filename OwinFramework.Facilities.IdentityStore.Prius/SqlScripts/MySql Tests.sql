set @identity='12345678';
set @secret='abcdef';
set @username='martin@gmail.com';
set @token='jhahjfgg';

call sp_AddIdentity(@identity, 'Create account', @identity);
call sp_AddCredential(@identity, 'Create account', @identity, @username, null, 1, 'HASH', 'SALT');
call sp_AddSharedSecret(@identity, 'Add API key', @identity, 'API Key', @secret, 'ReadContacts');
call sp_AddClaim(@identity, 'Add claim', @identity, 'email', @username, 1);

call sp_GetIdentity(@identity);
call sp_GetIdentitySharedSecrets(@identity);
call sp_GetIdentitySharedSecrets('invalid');
call sp_GetUserNameCredential(@username);
call sp_GetUserNameCredential('invalid');
call sp_GetSharedSecret(@secret);
call sp_GetSharedSecret('invalid');
call sp_GetIdentityClaims(@identity);

call sp_GetUserNameCredential(@username);
call sp_AuthenticateSuccess(@identity, null, @token, 'Credentials', 1, null);
call sp_GetIdentityAuthentications(@identity);

call sp_AuthenticateFail(@identity, 'Credentials', 1, @fail_count);
select @fail_count;

call sp_LockUsername(@username);
call sp_UnlockUsername(@username);

call sp_GetAuthenticationToken(@token);

call sp_DeleteIdentityCredentials(@identity, 'Change password', @identity);
call sp_DeleteSharedSecret(@identity, 'Revoke API key', @secret);

call sp_GetAudit(@identity);
